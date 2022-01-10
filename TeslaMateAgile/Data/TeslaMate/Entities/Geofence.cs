using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.TeslaMate.Entities;

[Table("geofences")]
public class Geofence
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [NotNull]
    public string? Name { get; set; }

    [Column("cost_per_unit")]
    public decimal? CostPerUnit { get; set; }
}
