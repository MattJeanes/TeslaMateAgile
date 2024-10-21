using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces;

public interface IDynamicPriceDataService : IPriceDataService
{
    Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to);
}
