using System;
using System.Threading.Tasks;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IWholePriceDataService
    {
        Task<decimal> GetTotalPrice(DateTimeOffset from, DateTimeOffset to);
    }
}
