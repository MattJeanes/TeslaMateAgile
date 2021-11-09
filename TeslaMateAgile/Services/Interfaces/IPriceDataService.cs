using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces;

public interface IPriceDataService
{
    Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to);
}
