using Microsoft.EntityFrameworkCore;
using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile.Data.TeslaMate;

public class TeslaMateDbContext : DbContext
{
    public TeslaMateDbContext(DbContextOptions<TeslaMateDbContext> options) : base(options) { }

    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<ChargingProcess> ChargingProcesses => Set<ChargingProcess>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
}
