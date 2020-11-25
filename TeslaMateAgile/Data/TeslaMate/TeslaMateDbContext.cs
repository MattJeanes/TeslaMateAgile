using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile.Data.TeslaMate
{
    public class TeslaMateDbContext : DbContext
    {
        public TeslaMateDbContext(DbContextOptions<TeslaMateDbContext> options) : base(options) { }
        public DbSet<Charge> Charges { get; set; }
        public DbSet<ChargingProcess> ChargingProcesses { get; set; }
    }
}
