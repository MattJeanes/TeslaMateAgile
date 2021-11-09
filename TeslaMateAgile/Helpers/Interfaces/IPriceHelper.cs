using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile.Helpers.Interfaces;

public interface IPriceHelper
{
    Task<(decimal Price, decimal Energy)> CalculateChargeCost(IEnumerable<Charge> charges);
    decimal CalculateEnergyUsed(IEnumerable<Charge> charges, decimal phases);
    Task Update();
}
