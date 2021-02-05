using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;

namespace TeslaMateAgile.Tests.Services
{
    public class FixedPriceServiceTests
    {
        public FixedPriceService Setup(List<string> prices)
        {
            var options = Options.Create(new FixedPriceOptions { Prices = prices });
            return new FixedPriceService(options);
        }

        private static readonly object[][] FixedPriceService_GetPriceData_Cases = new object[][] {
            new object[]
            {
                "PreviousNight",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T03:00:00"),
                DateTimeOffset.Parse("2021-02-01T18:00:00"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-01-31T20:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T03:30:00"),
                        Value = 4M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T03:30:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T06:00:00"),
                        Value = 3.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T06:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T08:00:00"),
                        Value = 2M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T08:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "MidDay_EdgeFrom",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T08:00:00"),
                DateTimeOffset.Parse("2021-02-01T18:00:00"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T08:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "MidDay_EdgeTo",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00"),
                DateTimeOffset.Parse("2021-02-01T20:00:00"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "MidDay_EdgeTo",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00"),
                DateTimeOffset.Parse("2021-02-02T07:00:00"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00"),
                        Value = 5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T20:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T03:30:00"),
                        Value = 4M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-02T03:30:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T06:00:00"),
                        Value = 3.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-02T06:00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T08:00:00"),
                        Value = 2M
                    }
                }
            }
        };

        [Test, TestCaseSource(nameof(FixedPriceService_GetPriceData_Cases))]
        public async Task FixedPriceService_GetPriceData(string testName, List<string> fixedPrices, DateTimeOffset from, DateTimeOffset to, List<Price> expectedPrices)
        {
            Console.WriteLine($"Running get price data test '{testName}'");
            var fixedPriceService = Setup(fixedPrices);
            var result = await fixedPriceService.GetPriceData(from, to);
            var actualPrices = result.OrderBy(x => x.ValidFrom).ToList();
            Assert.AreEqual(expectedPrices.Count, actualPrices.Count());
            for (var i = 0; i < actualPrices.Count(); i++)
            {
                var actualPrice = actualPrices[i];
                var expectedPrice = expectedPrices[i];

                Assert.AreEqual(expectedPrice.ValidFrom, actualPrice.ValidFrom);
                Assert.AreEqual(expectedPrice.ValidTo, actualPrice.ValidTo);
                Assert.AreEqual(expectedPrice.Value, actualPrice.Value);
            }
        }
    }
}
