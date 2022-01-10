using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeslaMateAgile.Data.TeslaMate.Entities;

[Table("charging_processes")]
public class ChargingProcess
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Do not use this property for date comparisons, it must be converted to a UTC DateTimeOffset first
    /// </summary>
    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("geofence_id")]
    public int GeofenceId { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("charge_energy_used")]
    public decimal? ChargeEnergyUsed { get; set; }

    public ICollection<Charge>? Charges { get; set; }
}
