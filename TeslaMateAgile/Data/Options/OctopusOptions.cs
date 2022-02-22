using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class OctopusOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public string ProductCode { get; set; }

    [Required]
    public string TariffCode { get; set; }

    [Required]
    public string RegionCode { get; set; }
}
