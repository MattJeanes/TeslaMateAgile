using System;
using System.Linq;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Octopus;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IOctopusService
    {
        Task<IOrderedEnumerable<AgilePrice>> GetAgilePrices(DateTime from, DateTime to);
    }
}