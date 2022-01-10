using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.Options;

public class BarryOptions
{
    [Required]
    [NotNull]
    public string? BaseUrl { get; set; }

    [Required]
    [NotNull]
    public string? ApiKey { get; set; }

    [Required]
    [NotNull]
    public string? MPID { get; set; }
}
