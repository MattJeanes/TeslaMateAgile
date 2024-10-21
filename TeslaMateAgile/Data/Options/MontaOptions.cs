using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class MontaOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; }

    public int? ChargePointId { get; set; }
}
