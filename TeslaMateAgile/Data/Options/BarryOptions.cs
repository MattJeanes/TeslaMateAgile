using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class BarryOptions
{
    [Required]
    public string BaseUrl { get; set; }
    
    [Required]
    public string ApiKey { get; set; }

    [Required]
    public string MPID { get; set; }
}
