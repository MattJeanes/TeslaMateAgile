using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class BarryOptions
{
    [Required]
    public string BaseUrl { get; set; }
    public string BarryApiKey { get; set; }
    public string BarryMPID { get; set; }
}
