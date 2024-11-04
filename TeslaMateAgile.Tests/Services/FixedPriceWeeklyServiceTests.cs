using Microsoft.Extensions.Options;
using NUnit.Framework;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;

namespace TeslaMateAgile.Tests.Services
{
    [TestFixture]
    public class FixedPriceWeeklyServiceTestsTest
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
                    "Sun=0.042"
                },
                new DateTimeOffset(new DateTime(2023, 1, 2, 2, 0, 0, DateTimeKind.Utc)), // Monday
                new DateTimeOffset(new DateTime(2023, 1, 10, 4, 0, 0, DateTimeKind.Utc)), // Next Tuesday
                new List<Price>
                {
                    // Monday prices from 2am
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 2, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 2, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Tuesday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 3, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 3, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Wednesday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 4, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 4, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Thursday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 5, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 5, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Friday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 6, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 6, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 7, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Saturday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 7, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 7, 18, 0, 0, DateTimeKind.Utc)), Value = 0.025m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 7, 18, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 7, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 7, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 8, 0, 0, 0, DateTimeKind.Utc)), Value = 0.025m },

                    // Sunday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 8, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc)), Value = 0.042m },

                    // Monday prices
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 6, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 8, 0, 0, DateTimeKind.Utc)), Value = 0.02m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 8, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 13, 0, 0, DateTimeKind.Utc)), Value = 0.1559m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 13, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 9, 20, 0, 0, DateTimeKind.Utc)), Value = 0.05m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 9, 20, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc)), Value = 0.04m },

                    // Tuesday prices until 4am
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 10, 3, 30, 0, DateTimeKind.Utc)), Value = 0.04m },
                    new() { ValidFrom = new DateTimeOffset(new DateTime(2023, 1, 10, 3, 30, 0, DateTimeKind.Utc)), ValidTo = new DateTimeOffset(new DateTime(2023, 1, 10, 6, 0, 0, DateTimeKind.Utc)), Value = 0.035m }
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
            Assert.That(actualPrices.Count(), Is.EqualTo(expectedPrices.Count));
            for (var i = 0; i < actualPrices.Count(); i++)
            {
                var actualPrice = actualPrices[i];
                var expectedPrice = expectedPrices[i];

                Assert.That(actualPrice.ValidFrom, Is.EqualTo(expectedPrice.ValidFrom));
                Assert.That(actualPrice.ValidTo, Is.EqualTo(expectedPrice.ValidTo));
                Assert.That(actualPrice.Value, Is.EqualTo(expectedPrice.Value));
            }
        }
    }
}
