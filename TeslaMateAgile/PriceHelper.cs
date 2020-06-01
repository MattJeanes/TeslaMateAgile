using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Octopus;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile
{
    public class PriceHelper : IPriceHelper
    {
        private readonly ILogger<PriceHelper> _logger;
        private readonly TeslaMateDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OctopusOptions _octopusOptions;
        private readonly TeslaMateOptions _teslaMateOptions;

        public PriceHelper(
            ILogger<PriceHelper> logger,
            TeslaMateDbContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<OctopusOptions> octopusOptions,
            IOptions<TeslaMateOptions> teslaMateOptions
            )
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _octopusOptions = octopusOptions.Value;
            _teslaMateOptions = teslaMateOptions.Value;
        }
        public async Task Update()
        {
            var httpClient = _httpClientFactory.CreateClient();
            _logger.LogInformation("Updating prices");

            var chargingProcesses = await _context.ChargingProcesses
                .Where(x => x.GeofenceId == _teslaMateOptions.GeofenceId && x.EndDate.HasValue && !x.Cost.HasValue)
                .Include(x => x.Charges)
                .ToListAsync();

            if (!chargingProcesses.Any())
            {
                _logger.LogInformation("No new charging processes");
                return;
            }

            foreach (var chargingProcess in chargingProcesses)
            {
                try
                {
                    var (cost, energy) = await CalculateChargeCost(httpClient, chargingProcess);
                    _logger.LogInformation($"Calculated cost {cost} and energy {energy} kWh for charging process {chargingProcess.Id}");
                    if (chargingProcess.ChargeEnergyUsed.HasValue && chargingProcess.ChargeEnergyUsed.Value != energy)
                    {
                        _logger.LogWarning($"Mismatch between TeslaMate calculated energy used of {chargingProcess.ChargeEnergyUsed.Value} and ours of {energy}");
                    }
                    chargingProcess.Cost = cost;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Failed to calculate charging cost / energy for charging process {chargingProcess.Id}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<(decimal Price, decimal Energy)> CalculateChargeCost(HttpClient httpClient, ChargingProcess chargingProcess)
        {
            var charges = chargingProcess.Charges;
            var minDate = charges.Min(x => x.Date);
            var maxDate = charges.Max(x => x.Date);
            var prices = await GetAgilePrices(httpClient, minDate, maxDate);
            var totalPrice = 0M;
            var totalEnergy = 0M;
            Charge lastCharge = null;
            foreach (var price in prices)
            {
                var chargesForPrice = charges.Where(x => x.Date >= price.ValidFrom && x.Date < price.ValidTo).ToList();
                if (lastCharge != null)
                {
                    chargesForPrice.Add(lastCharge);
                }
                chargesForPrice = chargesForPrice.OrderBy(x => x.Date).ToList();
                var energyAddedInDateRange = CalculateEnergyUsed(chargesForPrice);
                var priceForEnergy = energyAddedInDateRange * (price.ValueIncVAT / 100);
                totalPrice += priceForEnergy;
                totalEnergy += energyAddedInDateRange;
                lastCharge = chargesForPrice.Last();
            }
            return (Math.Round(totalPrice, 2), Math.Round(totalEnergy, 2));
        }

        private async Task<IOrderedEnumerable<AgilePrice>> GetAgilePrices(HttpClient httpClient, DateTime from, DateTime to)
        {
            var url = $"{_octopusOptions.AgileUrl}?period_from={from:o}&period_to={to:o}";
            var list = new List<AgilePrice>();
            do
            {
                var resp = await httpClient.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                var agileResponse = await JsonSerializer.DeserializeAsync<AgileResponse>(await resp.Content.ReadAsStreamAsync());
                list.AddRange(agileResponse.Results);
                url = agileResponse.Next;
                if (string.IsNullOrEmpty(url))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(5000); // back off API so they don't ban us
                }
            }
            while (true);
            return list.OrderBy(x => x.ValidFrom);
        }

        private decimal CalculateEnergyUsed(IEnumerable<Charge> charges)
        {
            // adapted from https://github.com/adriankumpf/teslamate/blob/0db6d6905ce804b3b8cafc0ab69aa8cd346446a8/lib/teslamate/log.ex#L464-L488
            var power = charges
                .Select(c => !c.ChargerPhases.HasValue ?
                    c.ChargerPower :
                     (c.ChargerActualCurrent * c.ChargerVoltage * _teslaMateOptions.Phases / 1000M)
                     * (charges.Any(x => x.Date < c.Date) ?
                        (decimal)(c.Date - charges.OrderByDescending(x => x.Date).FirstOrDefault(x => x.Date < c.Date).Date).TotalHours
                        : (decimal?)null)
                    );

            return power
                .Where(x => x.HasValue && x.Value >= 0)
                .Sum(x => x.Value);
        }

        private class AgileResponse
        {
            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("next")]
            public string Next { get; set; }

            [JsonPropertyName("previous")]
            public string Previous { get; set; }

            [JsonPropertyName("results")]
            public List<AgilePrice> Results { get; set; }
        }
    }
}
