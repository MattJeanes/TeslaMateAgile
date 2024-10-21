using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IWholePriceDataService : IPriceDataService
    {
        Task<IEnumerable<ProviderCharge>> GetTotalPrice(DateTimeOffset from, DateTimeOffset to);
    }
}
