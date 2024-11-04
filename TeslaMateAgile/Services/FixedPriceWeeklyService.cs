using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;
using TimeZoneConverter;

namespace TeslaMateAgile.Services;

public class FixedPriceWeeklyService : IDynamicPriceDataService
{
    private readonly Dictionary<DayOfWeek, List<FixedPriceWeekly>> _fixedPrices;

    public FixedPriceWeeklyService(
        IOptions<FixedPriceWeeklyOptions> options
        )
    {
        _fixedPrices = GetFixedPrices(options.Value);
        if (!TZConvert.TryGetTimeZoneInfo(options.Value.TimeZone, out _timeZone))
        {
            throw new ArgumentException(nameof(options.Value.TimeZone), $"Invalid TimeZone {options.Value.TimeZone}");
        }
    }

    public Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
    {
        var prices = new List<Price>();

        // Get all days between the range inclusive

        var fromDate = from.Date;
        var toDate = to.Date;
        var days = new Dictionary<DateTimeOffset, List<Price>>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            prices.AddRange(GetPriceDataForDate(date));
        }

        // Truncate the prices to the requested range, inclusive

        prices = prices.Where(x => x.ValidFrom < to && x.ValidTo > from).ToList();

        return Task.FromResult((IEnumerable<Price>)prices);
    }

    private List<Price> GetPriceDataForDate(DateTime date)
    {
        // Get all fixed prices for the day

        var prices = new List<Price>();

        var fixedPricesForDay = _fixedPrices[date.DayOfWeek];

        decimal? crossoverPrice = null;
        foreach (var fixedPrice in fixedPricesForDay)
        {
            var validFrom = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            var validTo = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            if (fixedPrice.FromHour.HasValue && fixedPrice.FromMinute.HasValue && fixedPrice.ToHour.HasValue && fixedPrice.ToMinute.HasValue)
            {
                validFrom = validFrom.AddHours(fixedPrice.FromHour.Value).AddMinutes(fixedPrice.FromMinute.Value);
                validTo = validTo.AddHours(fixedPrice.ToHour.Value).AddMinutes(fixedPrice.ToMinute.Value);
            }
            else
            {
                validTo = validTo.AddDays(1);
            }

            // Handle the scenario where they cross midnight

            if (validFrom > validTo)
            {
                validFrom = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                crossoverPrice = fixedPrice.Value;
            }

            var price = new Price
            {
                ValidFrom = validFrom.Add(-_timeZone.GetUtcOffset(validFrom)),
                ValidTo = validTo.Add(-_timeZone.GetUtcOffset(validTo)),
                Value = fixedPrice.Value
            };
            prices.Add(price);
        }

        // Ensure we have the last price of the day to cover the entire day

        prices = prices.OrderBy(x => x.ValidFrom).ToList();

        if (crossoverPrice.HasValue)
        {
            var lastPrice = prices.Last();
            var validTo = DateTime.SpecifyKind(date.AddDays(1), DateTimeKind.Utc);
            var price = new Price
            {
                ValidFrom = lastPrice.ValidTo,
                ValidTo = validTo.Add(-_timeZone.GetUtcOffset(validTo)),
                Value = crossoverPrice.Value
            };
            prices.Add(price);
        }

        // Verify that the prices cover the entire day

        if (prices.First().ValidFrom > DateTime.SpecifyKind(date, DateTimeKind.Utc))
        {
            throw new Exception("Invalid fixed price data, does not cover the entire day");
        }

        if (prices.Last().ValidTo < DateTime.SpecifyKind(date.AddDays(1), DateTimeKind.Utc))
        {
            throw new Exception("Invalid fixed price data, does not cover the entire day");
        }

        // Verify that the prices are continuous and do not overlap or have gaps

        for (var i = 1; i < prices.Count; i++)
        {
            if (prices[i - 1].ValidTo != prices[i].ValidFrom)
            {
                throw new Exception("Invalid fixed price data, prices are not continuous");
            }
        }

        return prices;
    }

    private class FixedPriceWeekly
    {
        public int? FromHour { get; set; }
        public int? FromMinute { get; set; }
        public int? ToHour { get; set; }
        public int? ToMinute { get; set; }
        public decimal Value { get; set; }
        public List<DayOfWeek> Days { get; set; }
    }

    private static readonly Regex FixedPriceWeeklyRegex = new Regex("(?<days>[a-zA-Z,-]+)(=(?<fromHour>\\d\\d):(?<fromMinute>\\d\\d)-(?<toHour>\\d\\d):(?<toMinute>\\d\\d))?=(?<value>.+)");
    private readonly TimeZoneInfo _timeZone;

    private Dictionary<DayOfWeek, List<FixedPriceWeekly>> GetFixedPrices(FixedPriceWeeklyOptions options)
    {
        var fixedPricesDict = new Dictionary<DayOfWeek, List<FixedPriceWeekly>>();

        foreach (var price in options.Prices)
        {
            var match = FixedPriceWeeklyRegex.Match(price);
            if (!match.Success)
            {
                throw new ArgumentException(nameof(price), $"Failed to parse fixed price: {price}");
            }

            var fromHour = match.Groups["fromHour"].Success ? int.Parse(match.Groups["fromHour"].Value) : (int?)null;
            var fromMinute = match.Groups["fromMinute"].Success ? int.Parse(match.Groups["fromMinute"].Value) : (int?)null;
            var toHour = match.Groups["toHour"].Success ? int.Parse(match.Groups["toHour"].Value) : (int?)null;
            var toMinute = match.Groups["toMinute"].Success ? int.Parse(match.Groups["toMinute"].Value) : (int?)null;

            if (!decimal.TryParse(match.Groups["value"].Value, out var value))
            {
                throw new ArgumentException(nameof(value), $"Failed to parse fixed price value: {match.Groups["value"].Value}");
            }

            var days = ParseDays(match.Groups["days"].Value);

            var fixedPrice = new FixedPriceWeekly
            {
                FromHour = fromHour,
                FromMinute = fromMinute,
                ToHour = toHour,
                ToMinute = toMinute,
                Value = value
            };

            foreach (var day in days)
            {
                if (!fixedPricesDict.ContainsKey(day))
                {
                    fixedPricesDict[day] = new List<FixedPriceWeekly>();
                }
                fixedPricesDict[day].Add(fixedPrice);
            }
        }

        return fixedPricesDict;
    }

    private List<DayOfWeek> ParseDays(string days)
    {
        var dayList = new List<DayOfWeek>();
        var dayRanges = days.Split(',');

        foreach (var dayRange in dayRanges)
        {
            if (dayRange.Contains('-'))
            {
                var rangeParts = dayRange.Split('-');
                var startDay = ParseDay(rangeParts[0]);
                var endDay = ParseDay(rangeParts[1]);

                for (var day = startDay; day <= endDay; day++)
                {
                    dayList.Add((DayOfWeek)day);
                }
            }
            else
            {
                dayList.Add(ParseDay(dayRange));
            }
        }

        return dayList;
    }

    private DayOfWeek ParseDay(string day)
    {
        return day.ToLower() switch
        {
            "mon" => DayOfWeek.Monday,
            "tue" => DayOfWeek.Tuesday,
            "wed" => DayOfWeek.Wednesday,
            "thu" => DayOfWeek.Thursday,
            "fri" => DayOfWeek.Friday,
            "sat" => DayOfWeek.Saturday,
            "sun" => DayOfWeek.Sunday,
            _ => throw new ArgumentException(nameof(day), $"Invalid day: {day}")
        };
    }
}
