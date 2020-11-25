using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests
{
    public class PriceHelperTests
    {
        public PriceHelper Setup(List<Price> prices)
        {
            var priceDataService = new Mock<IPriceDataService>();
            priceDataService
                .Setup(x => x.GetPriceData(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(prices.OrderBy(x => x.ValidFrom));

            var teslaMateDbContext = new Mock<TeslaMateDbContext>(new DbContextOptions<TeslaMateDbContext>());

            var logger = new ServiceCollection()
                .AddLogging(x => x.AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILogger<PriceHelper>>();

            var teslaMateOptions = Options.Create(new TeslaMateOptions { Phases = 1 });

            return new PriceHelper(logger, teslaMateDbContext.Object, priceDataService.Object, teslaMateOptions);
        }

        private static readonly object[][] PriceHelper_CalculateChargeCost_Cases = new object[][] {
            new object[]
            {
                "Plunge",
                ImportAgilePrices("plunge_test.json"),
                ImportCharges("plunge_test.csv"),
                -2.00M,
                36.74M,
            }
        };

        [Test]
        [TestCaseSource(nameof(PriceHelper_CalculateChargeCost_Cases))]
        public async Task PriceHelper_CalculateChargeCost(string testName, List<Price> prices, List<Charge> charges, decimal expectedPrice, decimal expectedEnergy)
        {
            Console.WriteLine($"Running calculate charge cost test '{testName}'");
            var priceHelper = Setup(prices);
            var (price, energy) = await priceHelper.CalculateChargeCost(charges);
            Assert.AreEqual(expectedPrice, price);
            Assert.AreEqual(expectedEnergy, energy);
        }

        private static List<Price> ImportAgilePrices(string jsonFile)
        {
            var json = File.ReadAllText(Path.Combine("Import", jsonFile));
            return JsonSerializer.Deserialize<OctopusService.AgileResponse>(json).Results
                .Select(x => new Price
                {
                    Value = x.ValueIncVAT,
                    ValidTo = x.ValidTo,
                    ValidFrom = x.ValidFrom
                }).ToList();
        }

        private static List<Charge> ImportCharges(string csvFile)
        {
            using var reader = new StreamReader(Path.Combine("Import", csvFile));
            using var parser = new CsvParser(reader, CultureInfo.InvariantCulture);
            using var csvReader = new CsvReader(parser);

            csvReader.Configuration.HasHeaderRecord = true;
            csvReader.Read();
            csvReader.ReadHeader();

            var charges = new List<Charge>();
            while (csvReader.Read())
            {
                charges.Add(new Charge
                {
                    Id = csvReader.GetField<int>("id"),
                    ChargeEnergyAdded = csvReader.GetField<decimal>("charge_energy_added"),
                    ChargerActualCurrent = csvReader.GetField<int>("charger_actual_current"),
                    ChargerPhases = csvReader.GetField<int>("charger_phases"),
                    ChargerPower = csvReader.GetField<int>("charger_power"),
                    ChargerVoltage = csvReader.GetField<int>("charger_voltage"),
#pragma warning disable CS0618 // Type or member is obsolete
                    DateInternal = csvReader.GetField<DateTime>("date")
#pragma warning restore CS0618 // Type or member is obsolete
                });
            }

            return charges;
        }
    }
}