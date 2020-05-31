using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using TeslaMateAgile.Data.TeslaMate.Entities;

namespace TeslaMateAgile.Data.TeslaMate
{
    public class TeslaMateDbContext : DbContext
    {
        public TeslaMateDbContext(DbContextOptions<TeslaMateDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.IsKeyless)
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }

        public DbSet<Charge> Charges { get; set; }
        public DbSet<ChargingProcess> ChargingProcesses { get; set; }
    }
}
