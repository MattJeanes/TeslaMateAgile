using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class NordpoolOptions
{
    [Required]
    public string BaseUrl { get; set; }
    
    [Required]
    public NordpoolCurrency Currency { get; set; }

    [Required]
    public string Region { get; set; }
}

public enum NordpoolCurrency
{
    DKK,
    EUR,
    NOK,
    SEK
}

