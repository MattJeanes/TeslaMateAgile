using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.Options;

public class FixedPriceOptions
{
    [Required]
    [NotNull]
    public string? TimeZone { get; set; }

    [Required]
    [NotNull]
    public List<string>? Prices { get; set; }
}
