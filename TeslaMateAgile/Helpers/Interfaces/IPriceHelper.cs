using System.Collections.Generic;
using System.Threading.Tasks;
using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile.Helpers.Interfaces
{
    public interface IPriceHelper
    {
        Task<(decimal Price, decimal Energy)> CalculateChargeCost(IEnumerable<Charge> charges);
        Task Update();
    }
}