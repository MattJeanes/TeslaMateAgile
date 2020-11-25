using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IPriceDataService
    {
        Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to);
    }
}