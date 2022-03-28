using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Data.TeslaMate.Entities;
using TeslaMateAgile.Helpers.Interfaces;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile;

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
        var geofence = await _context.Geofences.FirstOrDefaultAsync(x => x.Id == _teslaMateOptions.GeofenceId);

        if (geofence == null)
        {
            _logger.LogWarning($"Configured geofence id does not exist in the TeslaMate database, make sure you have entered the correct id");
            return;
        }
        else if (geofence.CostPerUnit.HasValue)
        {
            _logger.LogWarning($"Configured geofence '{geofence.Name}' (id: {geofence.Id}) should not have a cost set in TeslaMate as this may override TeslaMateAgile calculation");
            return;
        }

        _logger.LogInformation($"Looking for finished charging processes with no cost set in the '{geofence.Name}' geofence (id: {geofence.Id})");

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
                if (chargingProcess.Charges == null) { _logger.LogError($"Could not find charges on charging process {chargingProcess.Id}"); continue; }
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
        _logger.LogInformation($"Calculating cost for charges {minDate.UtcDateTime} UTC - {maxDate.UtcDateTime} UTC");
        var prices = (await _priceDataService.GetPriceData(minDate, maxDate)).OrderBy(x => x.ValidFrom);

        var opt = new JsonSerializerOptions() { WriteIndented = true };
        string strJson = JsonSerializer.Serialize<IEnumerable<Price>>(prices, opt);
        Console.WriteLine(strJson);

        var totalPrice = 0M;
        var totalEnergy = 0M;
        Charge lastCharge = null;
        var chargesCalculated = 0;
        var phases = DeterminePhases(charges);
        if (!phases.HasValue)
        {
            _logger.LogWarning($"Unable to determine phases for charges");
            return (0, 0);
        }
        foreach (var price in prices)
        {
            var chargesForPrice = charges.Where(x => x.Date >= price.ValidFrom && x.Date < price.ValidTo).ToList();
            chargesCalculated += chargesForPrice.Count;
            if (lastCharge != null)
            {
                chargesForPrice.Add(lastCharge);
            }
            chargesForPrice = chargesForPrice.OrderBy(x => x.Date).ToList();
            var energyAddedInDateRange = CalculateEnergyUsed(chargesForPrice, phases.Value);
            var priceForEnergy = (energyAddedInDateRange * price.Value) + (energyAddedInDateRange * _teslaMateOptions.FeePerKilowattHour);
            totalPrice += priceForEnergy;
            totalEnergy += energyAddedInDateRange;
            lastCharge = chargesForPrice.Last();
            _logger.LogDebug($"Calculated charge cost for {price.ValidFrom.UtcDateTime} UTC - {price.ValidTo.UtcDateTime} UTC (unit cost: {price.Value}, fee per kWh: {_teslaMateOptions.FeePerKilowattHour}): {priceForEnergy} for {energyAddedInDateRange} energy");
        }
        var chargesCount = charges.Count();
        if (chargesCalculated != chargesCount)
        {
            throw new Exception($"Charge calculation failed, pricing calculated for {chargesCalculated} / {chargesCount}, likely missing price data");
        }
        return (Math.Round(totalPrice, 2), Math.Round(totalEnergy, 2));
    }

    public decimal CalculateEnergyUsed(IEnumerable<Charge> charges, decimal phases)
    {
        // adapted from https://github.com/adriankumpf/teslamate/blob/0db6d6905ce804b3b8cafc0ab69aa8cd346446a8/lib/teslamate/log.ex#L464-L488
        var power = charges
            .Select(c => !c.ChargerPhases.HasValue ?
                c.ChargerPower :
                 ((c.ChargerActualCurrent ?? 0) * (c.ChargerVoltage ?? 0) * phases / 1000M)
                 * (charges.Any(x => x.Date < c.Date) ?
                    (decimal)(c.Date - charges.OrderByDescending(x => x.Date).First(x => x.Date < c.Date).Date).TotalHours
                    : (decimal?)null)
                );

        return power
            .Where(x => x.HasValue && x.Value >= 0)
            .Sum(x => x.Value);
    }

    public decimal? DeterminePhases(IEnumerable<Charge> charges)
    {
        // adapted from https://github.com/adriankumpf/teslamate/blob/0db6d6905ce804b3b8cafc0ab69aa8cd346446a8/lib/teslamate/log.ex#L490-L527
        var powerAverage = charges.Where(x => x.ChargerActualCurrent.HasValue && x.ChargerVoltage.HasValue)
                .Select(x => x.ChargerPower * 1000.0 / (x.ChargerActualCurrent.Value * x.ChargerVoltage.Value))
                .Where(x => !double.IsNaN(x))
                .Average();
        var phasesAverage = (int)charges.Where(x => x.ChargerPhases.HasValue).Average(x => x.ChargerPhases.Value);
        var voltageAverage = charges.Where(x => x.ChargerVoltage.HasValue).Average(x => x.ChargerVoltage.Value);
        if (powerAverage > 0 && charges.Count() > 15)
        {
            if (phasesAverage == Math.Round(powerAverage))
            {
                return phasesAverage;
            }
            if (phasesAverage == 3 && Math.Abs(powerAverage / Math.Sqrt(phasesAverage) - 1) <= 0.1)
            {
                _logger.LogInformation($"Voltage correction: {Math.Round(voltageAverage)}V -> {Math.Round(voltageAverage / Math.Sqrt(phasesAverage))}V");
                return (decimal)Math.Sqrt(phasesAverage);
            }
            if (Math.Abs(Math.Round(powerAverage) - powerAverage) <= 0.3)
            {
                _logger.LogInformation($"Phase correction: {phasesAverage} -> {Math.Round(powerAverage)}");
                return (decimal)Math.Round(powerAverage);
            }
        }
        return null;
    }
}
