using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.Options;

public class AwattarOptions
{
    [Required]
    [NotNull]
    public string? BaseUrl { get; set; }

    public decimal VATMultiplier { get; set; }
}
