using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using TeslaMateAgile.Data.Octopus;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Services;
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

            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>();

            var config = configBuilder.Build();

            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddHttpClient<IOctopusService, OctopusService>();
            services.Configure<OctopusOptions>(config.GetSection("Octopus"));

            //var octopusServiceReal = services.BuildServiceProvider().GetRequiredService<IOctopusService>();

            var teslaMateDbContext = new Mock<TeslaMateDbContext>(new DbContextOptions<TeslaMateDbContext>());

            var logger = new ServiceCollection()
                .AddLogging(x => x.AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILogger<PriceHelper>>();

            var teslaMateOptions = Options.Create(new TeslaMateOptions { Phases = 1 });

            return new PriceHelper(logger, teslaMateDbContext.Object, octopusService.Object, teslaMateOptions);
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
        public async Task PriceHelper_CalculateChargeCost(string testName, List<AgilePrice> agilePrices, List<Charge> charges, decimal expectedPrice, decimal expectedEnergy)
        {
            Console.WriteLine($"Running calculate charge cost test '{testName}'");
            var priceHelper = Setup(agilePrices);
            var (price, energy) = await priceHelper.CalculateChargeCost(charges);
            Assert.AreEqual(expectedPrice, price);
            Assert.AreEqual(expectedEnergy, energy);
        }

        private static List<AgilePrice> GenerateAgilePrices(string fromStr, string toStr, List<decimal> prices)
        {
            var from = DateTime.Parse(fromStr).ToUniversalTime();
            var to = DateTime.Parse(toStr).ToUniversalTime();
            var agilePrices = new List<AgilePrice>();
            var lastDate = from;
            var index = 0;
            while (lastDate < to)
            {
                var newDate = lastDate.AddMinutes(30);
                agilePrices.Add(new AgilePrice
                {
                    ValueIncVAT = prices[index],
                    ValidFrom = lastDate,
                    ValidTo = newDate
                });
                lastDate = newDate;
                index++;
            }

            return agilePrices;
        }

        private static List<AgilePrice> ImportAgilePrices(string jsonFile)
        {
            var json = File.ReadAllText(Path.Combine("Import", jsonFile));
            return JsonSerializer.Deserialize<AgileResponse>(json).Results;
        }

        private static List<Charge> GenerateCharges(string fromStr, string toStr)
        {
            var from = DateTime.Parse(fromStr).ToUniversalTime();
            var to = DateTime.Parse(toStr).ToUniversalTime();

            var charges = new List<Charge>
            {
                new Charge
                {
                    ChargerActualCurrent = 30,
                    ChargerVoltage = 240,
                    ChargerPhases = 1,
                    Date = from
                }
            };
            var lastDate = from;
            var rand = new Random();
            while (lastDate < to)
            {
                lastDate = lastDate.AddMinutes(rand.NextDouble());
                charges.Add(new Charge
                {
                    ChargerActualCurrent = 30,
                    ChargerVoltage = 240,
                    ChargerPhases = 1,
                    Date = lastDate
                });
            }
            charges.Add(new Charge
            {
                ChargerActualCurrent = 30,
                ChargerVoltage = 240,
                ChargerPhases = 1,
                Date = to.AddMilliseconds(-1)
            });

            return charges;
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
                    Date = DateTime.SpecifyKind(csvReader.GetField<DateTime>("date"), DateTimeKind.Utc).ToUniversalTime()
                });
            }

            return charges;
        }
    }
}