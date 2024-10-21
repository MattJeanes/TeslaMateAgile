using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            _logger.LogWarning("Configured geofence id does not exist in the TeslaMate database, make sure you have entered the correct id");
            return;
        }
        else if (geofence.CostPerUnit.HasValue)
        {
            _logger.LogWarning("Configured geofence '{Name}' (id: {Id}) should not have a cost set in TeslaMate as this may override TeslaMateAgile calculation", geofence.Name, geofence.Id);
            return;
        }

        var query = _context.ChargingProcesses
            .Include(x => x.Charges)
            .Where(x => x.GeofenceId == _teslaMateOptions.GeofenceId && x.EndDate.HasValue && !x.Cost.HasValue);

        if (_teslaMateOptions.LookbackDays.HasValue)
        {
            _logger.LogInformation("Looking for finished charging processes with no cost set started less than {Days} day(s) ago in the '{Name}' geofence (id: {Id})", _teslaMateOptions.LookbackDays.Value, geofence.Name, geofence.Id);
            query = query.Where(x => x.StartDate > DateTime.UtcNow.AddDays(-_teslaMateOptions.LookbackDays.Value));
        }
        else
        {
            _logger.LogInformation("Looking for finished charging processes with no cost set in the '{Name}' geofence (id: {Id})", geofence.Name, geofence.Id);
        }

        var chargingProcesses = await query.ToListAsync();

        if (!chargingProcesses.Any())
        {
            _logger.LogInformation("No new charging processes");
            return;
        }

        foreach (var chargingProcess in chargingProcesses)
        {
            try
            {
                if (chargingProcess.Charges == null) { _logger.LogError("Could not find charges on charging process {Id}", chargingProcess.Id); continue; }
                var (cost, energy) = await CalculateChargeCost(chargingProcess.Charges);
                _logger.LogInformation("Calculated cost {Cost} and energy {Energy} kWh for charging process {Id}", cost, energy, chargingProcess.Id);
                if (chargingProcess.ChargeEnergyUsed.HasValue && chargingProcess.ChargeEnergyUsed.Value != energy)
                {
                    _logger.LogWarning("Mismatch between TeslaMate calculated energy used of {ChargeEnergyUsed} and ours of {Energy}", chargingProcess.ChargeEnergyUsed.Value, energy);
                }
                chargingProcess.Cost = cost;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to calculate charging cost / energy for charging process {Id}", chargingProcess.Id);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<(decimal Price, decimal Energy)> CalculateChargeCost(IEnumerable<Charge> charges)
    {
        var minDate = charges.Min(x => x.Date);
        var maxDate = charges.Max(x => x.Date);
        _logger.LogInformation("Calculating cost for charges {MinDate} UTC - {MaxDate} UTC", minDate.UtcDateTime, maxDate.UtcDateTime);


        return _priceDataService switch
        {
            IDynamicPriceDataService => await CalculateDynamicChargeCost(charges, minDate, maxDate),
            IWholePriceDataService => await CalculateWholeChargeCost(charges, minDate, maxDate),
            _ => throw new ArgumentOutOfRangeException(nameof(_priceDataService), "Unknown price data service")
        };
    }

    private async Task<(decimal Price, decimal Energy)> CalculateDynamicChargeCost(IEnumerable<Charge> charges, DateTimeOffset minDate, DateTimeOffset maxDate)
    {
        var dynamicPriceDataService = _priceDataService as IDynamicPriceDataService;
        var prices = (await dynamicPriceDataService.GetPriceData(minDate, maxDate)).OrderBy(x => x.ValidFrom);

        _logger.LogDebug("Retrieved {Count} prices:", prices.Count());
        foreach (var price in prices)
        {
            _logger.LogDebug("{ValidFrom} UTC - {ValidTo} UTC: {Value}", price.ValidFrom.UtcDateTime, price.ValidTo.UtcDateTime, price.Value);
        }

        var totalChargePrice = 0M;
        var totalChargeEnergy = 0M;
        Charge lastCharge = null;
        var chargesCalculated = 0;
        var phases = ((decimal?)_teslaMateOptions.Phases) ?? DeterminePhases(charges);
        if (!phases.HasValue)
        {
            _logger.LogWarning("Unable to determine phases for charges");
            return (0, 0);
        }
        foreach (var price in prices)
        {
            var chargesForPrice = charges.Where(x => x.Date >= price.ValidFrom && x.Date <= price.ValidTo).ToList();
            if (chargesForPrice.Count == 0)
            {
                continue;
            }
            chargesCalculated += chargesForPrice.Count;
            if (lastCharge != null)
            {
                chargesForPrice.Add(lastCharge);
            }
            chargesForPrice = chargesForPrice.OrderBy(x => x.Date).ToList();
            var energyAddedInDateRange = CalculateEnergyUsed(chargesForPrice, phases.Value);
            var priceForEnergy = (energyAddedInDateRange * price.Value) + (energyAddedInDateRange * _teslaMateOptions.FeePerKilowattHour);
            totalChargePrice += priceForEnergy;
            totalChargeEnergy += energyAddedInDateRange;
            lastCharge = chargesForPrice.Last();
            _logger.LogDebug("Calculated charge cost for {ValidFrom} UTC - {ValidTo} UTC (unit cost: {Cost}, fee per kWh: {FeePerKilowattHour}): {PriceForEnergy} for {EnergyAddedInDateRange} energy",
                price.ValidFrom.UtcDateTime, price.ValidTo.UtcDateTime, price.Value, _teslaMateOptions.FeePerKilowattHour, priceForEnergy, energyAddedInDateRange);
        }
        var chargesCount = charges.Count();
        if (chargesCalculated != chargesCount)
        {
            throw new Exception($"Charge calculation failed, pricing calculated for {chargesCalculated} / {chargesCount}, likely missing price data");
        }
        return (Math.Round(totalChargePrice, 2), Math.Round(totalChargeEnergy, 2));
    }

    private async Task<(decimal Price, decimal Energy)> CalculateWholeChargeCost(IEnumerable<Charge> charges, DateTimeOffset minDate, DateTimeOffset maxDate)
    {
        var wholePriceDataService = _priceDataService as IWholePriceDataService;
        var searchMinDate = minDate.AddMinutes(-_teslaMateOptions.MatchingToleranceMinutes);
        var searchMaxDate = maxDate.AddMinutes(_teslaMateOptions.MatchingToleranceMinutes);
        var possibleCharges = await wholePriceDataService.GetCharges(searchMinDate, searchMaxDate);
        var mostAppropriateCharge = LocateMostAppropriateCharge(possibleCharges, minDate, maxDate);
        var wholeChargeEnergy = CalculateEnergyUsed(charges, ((decimal?)_teslaMateOptions.Phases) ?? DeterminePhases(charges).Value);
        return (Math.Round(mostAppropriateCharge.Cost, 2), Math.Round(wholeChargeEnergy, 2));
    }

    private ProviderCharge LocateMostAppropriateCharge(IEnumerable<ProviderCharge> possibleCharges, DateTimeOffset minDate, DateTimeOffset maxDate)
    {
        var tolerance = _teslaMateOptions.MatchingToleranceMinutes;

        var appropriateCharges = possibleCharges
            .Where(pc => pc.StartTime >= minDate.AddMinutes(-tolerance) && pc.EndTime <= maxDate.AddMinutes(tolerance))
            .OrderBy(pc => Math.Min(Math.Abs((pc.StartTime - minDate).TotalMinutes), Math.Abs((pc.EndTime - maxDate).TotalMinutes)))
            .ToList();

        if (!appropriateCharges.Any())
        {
            throw new Exception("No appropriate charge found within the tolerance range.");
        }

        return appropriateCharges.First();
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
        var powerAverages = charges.Where(x => x.ChargerActualCurrent.HasValue && x.ChargerVoltage.HasValue)
                .Select(x => x.ChargerPower * 1000.0 / (x.ChargerActualCurrent.Value * x.ChargerVoltage.Value))
                .Where(x => !double.IsNaN(x));
        if (!powerAverages.Any())
        {
            _logger.LogWarning($"No charges with power data");
            return null;
        }
        var powerAverage = powerAverages.Average();
        if (!charges.Any(x => x.ChargerPhases.HasValue))
        {
            _logger.LogWarning($"No charges with phase data");
            return null;
        }
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
                _logger.LogInformation("Voltage correction: {VoltageAverage}V -> {CorrectedVoltageAverage}V", Math.Round(voltageAverage), Math.Round(voltageAverage / Math.Sqrt(phasesAverage)));
                return (decimal)Math.Sqrt(phasesAverage);
            }
            if (Math.Abs(Math.Round(powerAverage) - powerAverage) <= 0.3)
            {
                _logger.LogInformation("Phase correction: {PhasesAverage} -> {CorrectedPhases}", phasesAverage, Math.Round(powerAverage));
                return (decimal)Math.Round(powerAverage);
            }
        }
        return null;
    }
}
