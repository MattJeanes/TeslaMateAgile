using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;
using TimeZoneConverter;

namespace TeslaMateAgile.Services;

public class FixedPriceWeeklyService : IDynamicPriceDataService
{
    private readonly List<FixedPriceWeekly> _fixedPrices;

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
        var started = false;
        var dayIndex = -1;
        int fpIndex = 0;
        var maxIterations = 100; // fail-safe against infinite loop
        for (var i = 0; i <= maxIterations; i++)
        {
            var fixedPrice = _fixedPrices[fpIndex];
            var validFrom = DateTime.SpecifyKind(from.Date.AddDays(dayIndex).AddHours(fixedPrice.FromHour).AddMinutes(fixedPrice.FromMinute), DateTimeKind.Utc);
            var validTo = DateTime.SpecifyKind(from.Date.AddDays(dayIndex).AddHours(fixedPrice.ToHour).AddMinutes(fixedPrice.ToMinute), DateTimeKind.Utc);
            var price = new Price
            {
                ValidFrom = validFrom.Add(-_timeZone.GetUtcOffset(validFrom)),
                ValidTo = validTo.Add(-_timeZone.GetUtcOffset(validTo)),
                Value = fixedPrice.Value
            };
            if (price.ValidFrom < to && price.ValidTo > from)
            {
                prices.Add(price);
                started = true;
            }
            else if (started)
            {
                break;
            }
            fpIndex++;
            if (fpIndex >= _fixedPrices.Count)
            {
                fpIndex = 0;
                dayIndex++;
            }
            if (i == maxIterations)
            {
                throw new Exception("Infinite loop detected within FixedPriceWeekly provider");
            }
        }

        return Task.FromResult(prices.AsEnumerable());
    }

    private class FixedPriceWeekly
    {
        public int FromHour { get; set; }
        public int FromMinute { get; set; }
        public int ToHour { get; set; }
        public int ToMinute { get; set; }
        public decimal Value { get; set; }
        public List<DayOfWeek> Days { get; set; }
    }

    private static readonly Regex FixedPriceWeeklyRegex = new Regex("(?<days>[a-zA-Z,-]+)=(?<fromHour>\\d\\d):(?<fromMinute>\\d\\d)-(?<toHour>\\d\\d):(?<toMinute>\\d\\d)=(?<value>.+)");
    private readonly TimeZoneInfo _timeZone;

    private List<FixedPriceWeekly> GetFixedPrices(FixedPriceWeeklyOptions options)
    {
        var fixedPrices = new List<FixedPriceWeekly>();

        foreach (var price in options.Prices.OrderBy(x => x))
        {
            var match = FixedPriceWeeklyRegex.Match(price);
            if (!match.Success) { throw new ArgumentException(nameof(price), $"Failed to parse fixed price: {price}"); }
            var fromHour = int.Parse(match.Groups["fromHour"].Value);
            var fromMinute = int.Parse(match.Groups["fromMinute"].Value);
            var toHour = int.Parse(match.Groups["toHour"].Value);
            var toMinute = int.Parse(match.Groups["toMinute"].Value);
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
                Value = value,
                Days = days
            };
            fixedPrices.Add(fixedPrice);
        }

        return fixedPrices;
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
