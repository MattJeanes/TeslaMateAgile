using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces;

public interface IDynamicPriceDataService
{
    Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to);
}
