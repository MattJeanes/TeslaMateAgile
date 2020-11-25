using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Helpers.Interfaces;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile
{
    public class PriceHelper : IPriceHelper
    {
        private readonly ILogger<PriceHelper> _logger;
        private readonly TeslaMateDbContext _context;
        private readonly IPriceDataService _priceDataService;
        private readonly TeslaMateOptions _teslaMateOptions;

        public PriceHelper(
            ILogger<PriceHelper> logger,
            TeslaMateDbContext context,
            IPriceDataService priceDataService,
            IOptions<TeslaMateOptions> teslaMateOptions
            )
        {
            _logger = logger;
            _context = context;
            _priceDataService = priceDataService;
            _teslaMateOptions = teslaMateOptions.Value;
        }
        public async Task Update()
        {
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
                    var (cost, energy) = await CalculateChargeCost(chargingProcess.Charges);
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

        public async Task<(decimal Price, decimal Energy)> CalculateChargeCost(IEnumerable<Charge> charges)
        {
            var minDate = charges.Min(x => x.Date);
            var maxDate = charges.Max(x => x.Date);
            var prices = (await _priceDataService.GetPriceData(minDate, maxDate)).OrderBy(x => x.ValidFrom);
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
                var priceForEnergy = energyAddedInDateRange * (price.Value / 100);
                totalPrice += priceForEnergy;
                totalEnergy += energyAddedInDateRange;
                lastCharge = chargesForPrice.Last();
            }
            return (Math.Round(totalPrice, 2), Math.Round(totalEnergy, 2));
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
    }
}
