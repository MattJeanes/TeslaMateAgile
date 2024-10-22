namespace TeslaMateAgile.Data;

public class ProviderCharge
{
    public decimal Cost { get; set; }
    public decimal? EnergyKwh { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}
