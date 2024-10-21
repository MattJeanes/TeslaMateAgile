using System;
using System.Threading.Tasks;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IWholePriceDataService : IPriceDataService
    {
        Task<decimal> GetTotalPrice(DateTimeOffset from, DateTimeOffset to);
    }
}
