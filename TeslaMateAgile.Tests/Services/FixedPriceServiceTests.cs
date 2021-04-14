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
        public FixedPriceService Setup(string timeZone, List<string> prices)
        {
            var options = Options.Create(new FixedPriceOptions { TimeZone = timeZone, Prices = prices });
            return new FixedPriceService(options);
        }

        private static readonly object[][] FixedPriceService_GetPriceData_Cases = new object[][] {
            new object[]
            {
                "PreviousNight",
                "Etc/UTC",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T03:00:00Z"),
                DateTimeOffset.Parse("2021-02-01T18:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-01-31T20:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T03:30:00Z"),
                        Value = 4M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T03:30:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T06:00:00Z"),
                        Value = 3.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T06:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T08:00:00Z"),
                        Value = 2M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T08:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "MidDay_EdgeFrom",
                "Etc/UTC",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T08:00:00Z"),
                DateTimeOffset.Parse("2021-02-01T18:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T08:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "MidDay_EdgeTo",
                "Etc/UTC",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00Z"),
                DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "NextNight",
                "Etc/UTC",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00Z"),
                DateTimeOffset.Parse("2021-02-02T07:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                        Value = 5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T20:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T03:30:00Z"),
                        Value = 4M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-02T03:30:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T06:00:00Z"),
                        Value = 3.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-02T06:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T08:00:00Z"),
                        Value = 2M
                    }
                }
            },
            new object[]
            {
                "TimeZone",
                "America/New_York",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00Z"),
                DateTimeOffset.Parse("2021-02-02T07:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T18:00:00Z"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T18:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T01:00:00Z"),
                        Value = 5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-02T01:00:00Z"),
                        ValidTo = DateTimeOffset.Parse("2021-02-02T08:30:00Z"),
                        Value = 4M
                    }
                }
            },
            new object[]
            {
                "TimeZone_DayEarlier",
                "America/New_York",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T03:00:00Z"),
                DateTimeOffset.Parse("2021-02-01T10:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-01-31T20:00:00-05:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T03:30:00-05:00"),
                        Value = 4M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T03:30:00-05:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T06:00:00-05:00"),
                        Value = 3.5M
                    }
                }
            },
            new object[]
            {
                "TimeZone_DayAfter",
                "America/New_York",
                new List<string>
                {
                    "08:00-13:00=1.5",
                    "13:00-20:00=5",
                    "20:00-03:30=4",
                    "03:30-06:00=3.5",
                    "06:00-08:00=2",
                },
                DateTimeOffset.Parse("2021-02-01T15:00:00Z"),
                DateTimeOffset.Parse("2021-02-01T23:00:00Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T08:00:00-05:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T13:00:00-05:00"),
                        Value = 1.5M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-02-01T13:00:00-05:00"),
                        ValidTo = DateTimeOffset.Parse("2021-02-01T20:00:00-05:00"),
                        Value = 5M
                    }
                }
            },
            new object[]
            {
                "CrossDay_TimeZone",
                "Europe/Paris",
                new List<string>
                {
                    "08:00-23:00=0.1799",
                    "23:00-02:00=0.1346",
                    "02:00-06:00=0.1095",
                    "06:00-08:00=0.1346"
                },
                DateTimeOffset.Parse("2021-01-26T00:39:45Z"),
                DateTimeOffset.Parse("2021-01-26T04:14:30Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-01-25T22:00:00+00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-01-26T01:00:00+00:00"),
                        Value = 0.1346M
                    },
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-01-26T01:00:00+00:00"),
                        ValidTo = DateTimeOffset.Parse("2021-01-26T05:00:00+00:00"),
                        Value = 0.1095M
                    }
                }
            },
            new object[]
            {
                "DaylightSavingsTime",
                "Europe/London",
                new List<string>
                {
                    "23:30-20:30=13.8",
                    "20:30-23:30=4.5"
                },
                DateTimeOffset.Parse("2021-04-13T19:46:13Z"),
                DateTimeOffset.Parse("2021-04-13T22:02:46Z"),
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-04-13T20:30:00+01:00"),
                        ValidTo = DateTimeOffset.Parse("2021-04-13T23:30:00+01:00"),
                        Value = 4.5M
                    }
                }
            }
        };

        [Test, TestCaseSource(nameof(FixedPriceService_GetPriceData_Cases))]
        public async Task FixedPriceService_GetPriceData(string testName, string timeZone, List<string> fixedPrices, DateTimeOffset from, DateTimeOffset to, List<Price> expectedPrices)
        {
            Console.WriteLine($"Running get price data test '{testName}'");
            var fixedPriceService = Setup(timeZone, fixedPrices);
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
