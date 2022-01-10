using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TeslaMateAgile.Data.Options;

public class TibberOptions
{
    [Required]
    [NotNull]
    public string? BaseUrl { get; set; }

    [Required]
    [NotNull]
    public string? AccessToken { get; set; }
}
