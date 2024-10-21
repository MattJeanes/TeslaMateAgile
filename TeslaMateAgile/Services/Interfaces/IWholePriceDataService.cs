using TeslaMateAgile.Data;

namespace TeslaMateAgile.Services.Interfaces
{
    public interface IWholePriceDataService : IPriceDataService
    {
        Task<IEnumerable<ProviderCharge>> GetCharges(DateTimeOffset from, DateTimeOffset to);
    }
}
