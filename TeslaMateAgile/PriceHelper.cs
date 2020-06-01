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
            }

            foreach (var chargingProcess in chargingProcesses)
            {
                var cost = await CalculateChargeCost(httpClient, chargingProcess);
                _logger.LogInformation($"Calculated cost {cost} for charging process {chargingProcess.Id}");
                chargingProcess.Cost = cost;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<decimal> CalculateChargeCost(HttpClient httpClient, ChargingProcess chargingProcess)
        {
            var charges = chargingProcess.Charges;
            var minDate = charges.Min(x => x.Date);
            var maxDate = charges.Max(x => x.Date);
            var prices = await GetAgilePrices(httpClient, minDate, maxDate);
            var totalPrice = 0M;
            decimal? lastEnergyAdded = null;
            foreach (var price in prices)
            {
                var chargesForPrice = charges.Where(x => x.Date >= price.ValidFrom && x.Date < price.ValidTo);
                var minEnergyAdded = lastEnergyAdded ?? chargesForPrice.Min(x => x.ChargeEnergyAdded);
                var maxEnergyAdded = chargesForPrice.Max(x => x.ChargeEnergyAdded);
                var energyAddedInDateRange = maxEnergyAdded - minEnergyAdded;
                var priceForEnergy = energyAddedInDateRange * price.ValueIncVAT;
                totalPrice += priceForEnergy;
                lastEnergyAdded = maxEnergyAdded;
            }
            return totalPrice;
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
