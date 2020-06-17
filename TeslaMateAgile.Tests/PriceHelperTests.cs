using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Octopus;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests
{
    public class PriceHelperTests
    {
        public PriceHelper Setup(List<AgilePrice> agilePrices)
        {
            var octopusService = new Mock<IOctopusService>();
            octopusService
                .Setup(x => x.GetAgilePrices(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(agilePrices.OrderBy(x => x.ValidFrom));

            var teslaMateDbContext = new Mock<TeslaMateDbContext>(new DbContextOptions<TeslaMateDbContext>());

            var logger = new Mock<ILogger<PriceHelper>>();

            var teslaMateOptions = Options.Create(new TeslaMateOptions { Phases = 1 });

            return new PriceHelper(logger.Object, teslaMateDbContext.Object, octopusService.Object, teslaMateOptions);
        }

        private static readonly object[][] PriceHelper_CalculateChargeCost_Cases = new object[][] {
            new object[]
            {
                "Test",
                new List<AgilePrice> {
                    new AgilePrice
                    {
                        ValueIncVAT = 0.05M,
                        ValidFrom = DateTime.Parse("2020-05-20 00:00:00"),
                        ValidTo = DateTime.Parse("2020-05-20 00:30:00")
                    },
                    new AgilePrice
                    {
                        ValueIncVAT = 0.02M,
                        ValidFrom = DateTime.Parse("2020-05-20 00:30:00"),
                        ValidTo = DateTime.Parse("2020-05-20 01:00:00")
                    },
                    new AgilePrice
                    {
                        ValueIncVAT = 0.01M,
                        ValidFrom = DateTime.Parse("2020-05-20 01:00:00"),
                        ValidTo = DateTime.Parse("2020-05-20 01:30:00")
                    },
                    new AgilePrice
                    {
                        ValueIncVAT = 0.03M,
                        ValidFrom = DateTime.Parse("2020-05-20 01:30:00"),
                        ValidTo = DateTime.Parse("2020-05-20 02:00:00")
                    }
                },
                new List<Charge> {
                    new Charge
                    {
                        ChargerActualCurrent = 32,
                        ChargerVoltage = 240,
                        Date = DateTime.Parse("2020-05-20 00:00:00")
                    },
                    new Charge
                    {
                        ChargerActualCurrent = 32,
                        ChargerVoltage = 240,
                        Date = DateTime.Parse("2020-05-20 02:00:00")
                    }
                },
                1.001M,
                1.002M
            }
        };

        [Test]
        [TestCaseSource(nameof(PriceHelper_CalculateChargeCost_Cases))]
        public async Task PriceHelper_CalculateChargeCost(string testName, List<AgilePrice> agilePrices, List<Charge> charges, decimal expectedPrice, decimal expectedEnergy)
        {
            Console.WriteLine($"Running calculate charge cost test '{testName}'");
            var priceHelper = Setup(agilePrices);
            var (price, energy) = await priceHelper.CalculateChargeCost(charges);
            Assert.AreEqual(expectedPrice, price);
            Assert.AreEqual(expectedEnergy, energy);
        }
    }
}