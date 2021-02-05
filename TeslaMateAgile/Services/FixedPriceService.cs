using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services
{
    public class FixedPriceService : IPriceDataService
    {
        private readonly List<FixedPrice> _fixedPrices;

        public FixedPriceService(
            IOptions<FixedPriceOptions> options
            )
        {
            _fixedPrices = GetFixedPrices(options.Value);
        }

        public Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
        {
            var prices = new List<Price>();
            var days = (to.Date - from.Date).Days;
            var lastPrice = _fixedPrices.Last();
            FixedPrice lastFixedPriceAdded = null;
            for (var i = 0; i <= days; i++)
            {
                var date = from.Date;
                if (!_fixedPrices.Any(x => x.FromHour < from.Hour) && (lastPrice != lastFixedPriceAdded))
                {
                    var price = new Price
                    {
                        ValidFrom = date.AddDays(i).AddHours(lastPrice.FromHour - 24).AddMinutes(lastPrice.FromMinute),
                        ValidTo = date.AddDays(i).AddHours(lastPrice.ToHour - 24).AddMinutes(lastPrice.ToMinute),
                        Value = lastPrice.Value
                    };
                    if (price.ValidFrom < to && price.ValidTo > from)
                    {
                        prices.Add(price);
                        lastFixedPriceAdded = lastPrice;
                    }
                }
                foreach (var fixedPrice in _fixedPrices)
                {
                    var price = new Price
                    {
                        ValidFrom = date.AddDays(i).AddHours(fixedPrice.FromHour).AddMinutes(fixedPrice.FromMinute),
                        ValidTo = date.AddDays(i).AddHours(fixedPrice.ToHour).AddMinutes(fixedPrice.ToMinute),
                        Value = fixedPrice.Value
                    };
                    if (price.ValidFrom < to && price.ValidTo > from)
                    {
                        prices.Add(price);
                        lastFixedPriceAdded = lastPrice;
                    }
                }
            }

            return Task.FromResult(prices.AsEnumerable());
        }

        private class FixedPrice
        {
            public int FromHour { get; set; }
            public int FromMinute { get; set; }
            public int ToHour { get; set; }
            public int ToMinute { get; set; }
            public decimal Value { get; set; }
        }

        private static readonly Regex FixedPriceRegex = new Regex("(\\d\\d):(\\d\\d)-(\\d\\d):(\\d\\d)=(.+)");
        private List<FixedPrice> GetFixedPrices(FixedPriceOptions options)
        {
            var fixedPrices = new List<FixedPrice>();
            var totalHours = 0M;
            FixedPrice lastFixedPrice = null;

            foreach (var price in options.Prices.OrderBy(x => x))
            {
                var match = FixedPriceRegex.Match(price);
                if (!match.Success) { throw new ArgumentException(nameof(price), $"Failed to parse fixed price: {price}"); }
                var fromHour = int.Parse(match.Groups[1].Value);
                var fromMinute = int.Parse(match.Groups[2].Value);
                var toHour = int.Parse(match.Groups[3].Value);
                var toMinute = int.Parse(match.Groups[4].Value);
                if (!decimal.TryParse(match.Groups[5].Value, out var value))
                {
                    throw new ArgumentException(nameof(value), $"Failed to parse fixed price value: {match.Groups[5].Value}");
                }
                var fromHours = fromHour + (fromMinute / 60M);
                var toHours = toHour + (toMinute / 60M);
                if (fromHours > toHours)
                {
                    toHours += 24;
                    toHour += 24;
                }
                var fixedPrice = new FixedPrice
                {
                    FromHour = fromHour,
                    FromMinute = fromMinute,
                    ToHour = toHour,
                    ToMinute = toMinute,
                    Value = value
                };
                fixedPrices.Add(fixedPrice);

                if (lastFixedPrice != null && (fixedPrice.FromHour != lastFixedPrice.ToHour || fixedPrice.FromMinute != lastFixedPrice.ToMinute))
                {
                    throw new ArgumentException(nameof(price), $"Price from time does not match previous to time: {price}");
                }
                totalHours += toHours - fromHours;
                lastFixedPrice = fixedPrice;
            }
            if (totalHours != 24)
            {
                throw new ArgumentException(nameof(totalHours), $"Total hours do not equal 24, currently {totalHours}");
            }
            return fixedPrices;
        }
    }
}
