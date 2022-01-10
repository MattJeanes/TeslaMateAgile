using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.Options;

public class OctopusOptions
{
    [Required]
    [NotNull]
    public string? BaseUrl { get; set; }

    [Required]
    [NotNull]
    public string? ProductCode { get; set; }

    [Required]
    [NotNull]
    public string? TariffCode { get; set; }

    [Required]
    [NotNull]
    public string? RegionCode { get; set; }
}
