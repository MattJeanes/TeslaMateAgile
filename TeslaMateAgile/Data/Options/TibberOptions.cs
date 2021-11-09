using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class TibberOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public string AccessToken { get; set; }
}
