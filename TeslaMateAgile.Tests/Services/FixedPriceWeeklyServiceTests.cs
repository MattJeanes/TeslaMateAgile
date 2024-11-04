using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;
using TeslaMateAgile.Data; // Added the necessary using directive for the `Price` type

namespace TeslaMateAgile.Tests.Services
{
    [TestFixture]
    public class FixedPriceWeeklyServiceTests
    {
        private FixedPriceWeeklyService Setup(string timeZone, List<string> prices)
        {
            var options = Options.Create(new FixedPriceWeeklyOptions { TimeZone = timeZone, Prices = prices });
            return new FixedPriceWeeklyService(options);
        }

        private static readonly object[][] FixedPriceWeeklyService_GetPriceData_Cases = new object[][]
        {
            new object[]
            {
                "WeekdaysAndWeekend",
                "Europe/London",
                new List<string>
                {
                    "Mon-Fri=08:00-13:00=0.1559",
                    "Mon-Fri=13:00-20:00=0.05",
                    "Mon-Fri=20:00-03:30=0.04",
                    "Mon-Fri=03:30-06:00=0.035",
                    "Mon-Fri=06:00-08:00=0.02",
                    "Sat=18:00-20:00=0.05",
                    "Sat=20:00-18:00=0.025",
                    "Sun=0.04"
                },
                new DateTimeOffset(new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc)), // Monday
                new DateTimeOffset(new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc)), // Next Monday
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 8, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 13, 0, 0, DateTimeKind.Utc)),
                        Value = 0.1559m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 13, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 20, 0, 0, DateTimeKind.Utc)),
                        Value = 0.05m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 20, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 3, 30, 0, DateTimeKind.Utc)),
                        Value = 0.04m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 3, 30, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 6, 0, 0, DateTimeKind.Utc)),
                        Value = 0.035m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 6, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 8, 0, 0, DateTimeKind.Utc)),
                        Value = 0.02m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 7, 18, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 7, 20, 0, 0, DateTimeKind.Utc)),
                        Value = 0.05m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 7, 20, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 8, 18, 0, 0, DateTimeKind.Utc)),
                        Value = 0.025m
                    },
                    new Price
                    {
                        ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 8, 0, 0, 0, DateTimeKind.Utc)),
                        ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc)),
                        Value = 0.04m
                    }
                }
            }
        };

        [Test, TestCaseSource(nameof(FixedPriceWeeklyService_GetPriceData_Cases))]
        public async Task FixedPriceWeeklyService_GetPriceData(string testName, string timeZone, List<string> fixedPrices, DateTimeOffset from, DateTimeOffset to, List<Price> expectedPrices)
        {
            Console.WriteLine($"Running get price data test '{testName}'");
            var fixedPriceWeeklyService = Setup(timeZone, fixedPrices);
            var result = await fixedPriceWeeklyService.GetPriceData(from, to);
            var actualPrices = result.OrderBy(x => x.ValidFrom).ToList();
            Assert.That(expectedPrices.Count, Is.EqualTo(actualPrices.Count()));
            for (var i = 0; i < actualPrices.Count(); i++)
            {
                var actualPrice = actualPrices[i];
                var expectedPrice = expectedPrices[i];

                Assert.That(expectedPrice.ValidFrom, Is.EqualTo(actualPrice.ValidFrom));
                Assert.That(expectedPrice.ValidTo, Is.EqualTo(actualPrice.ValidTo));
                Assert.That(expectedPrice.Value, Is.EqualTo(actualPrice.Value));
            }
        }
    }
}
