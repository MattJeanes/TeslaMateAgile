using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests;

public class PriceHelperTests
{
    public PriceHelper Setup(List<Price> prices = null)
    {
        if (prices == null) { prices = new List<Price>(); }

        var priceDataService = new Mock<IPriceDataService>();
        priceDataService
            .Setup(x => x.GetPriceData(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(prices.OrderBy(x => x.ValidFrom));

        var teslaMateDbContext = new Mock<TeslaMateDbContext>(new DbContextOptions<TeslaMateDbContext>());

        var logger = new ServiceCollection()
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<PriceHelper>>();

        var teslaMateOptions = Options.Create(new TeslaMateOptions());

        return new PriceHelper(logger, teslaMateDbContext.Object, priceDataService.Object, teslaMateOptions);
    }

    private static readonly object[][] PriceHelper_CalculateChargeCost_Cases = new object[][] {
            new object[]
            {
                "Plunge",
                TestHelpers.ImportAgilePrices("plunge_test.json"),
                TestHelpers.ImportCharges("plunge_test.csv"),
                -2.00M,
                36.74M,
            },
            new object[]
            {
                "DaylightSavingsTime",
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2021-04-13T20:30:00+01:00"),
                        ValidTo = DateTimeOffset.Parse("2021-04-13T23:30:00+01:00"),
                        Value = 4.5M
                    }
                },
                TestHelpers.ImportCharges("daylightsavingstime_test.csv"),
                75.5M,
                16.78M,
            },
            new object[]
            {
                "ExactMillisecond",
                new List<Price>
                {
                    new Price
                    {
                        ValidFrom = DateTimeOffset.Parse("2023-08-24T23:43:53.026Z"),
                        ValidTo =   DateTimeOffset.Parse("2023-08-25T03:19:42.588Z"),
                        Value = 0.2748M
                    }
                },
                TestHelpers.ImportCharges("exactmillisecond_test.csv"),
                5.88M,
                21.41M,
            }
        };

    [Test]
    [TestCaseSource(nameof(PriceHelper_CalculateChargeCost_Cases))]
    public async Task PriceHelper_CalculateChargeCost(string testName, List<Price> prices, List<Charge> charges, decimal expectedPrice, decimal expectedEnergy)
    {
        Console.WriteLine($"Running calculate charge cost test '{testName}'");
        var priceHelper = Setup(prices);
        var (price, energy) = await priceHelper.CalculateChargeCost(charges);
        Assert.That(expectedPrice, Is.EqualTo(price));
        Assert.That(expectedEnergy, Is.EqualTo(energy));
    }

    private static readonly object[][] PriceHelper_CalculateEnergyUsed_Cases = new object[][] {
            new object[]
            {
                "ThreePhase",
                TestHelpers.ImportCharges("threephase_test.csv"),
                47.65M,
            }
        };

    [Test]
    [TestCaseSource(nameof(PriceHelper_CalculateEnergyUsed_Cases))]
    public void PriceHelper_CalculateEnergyUsed(string testName, List<Charge> charges, decimal expectedEnergy)
    {
        Console.WriteLine($"Running calculate energy used test '{testName}'");
        var priceHelper = Setup();
        var phases = priceHelper.DeterminePhases(charges);
        if (!phases.HasValue) { throw new Exception("Phases has no value"); }
        var energy = priceHelper.CalculateEnergyUsed(charges, phases.Value);
        Assert.That(expectedEnergy, Is.EqualTo(Math.Round(energy, 2)));
    }

    [Test]
    public async Task PriceHelper_NoPhaseData()
    {
        var charges = TestHelpers.ImportCharges("nophasedata_test.csv");
        var priceHelper = Setup();
        var (price, energy) = await priceHelper.CalculateChargeCost(charges);
        Assert.That(0, Is.EqualTo(price));
        Assert.That(0, Is.EqualTo(energy));
    }
}
