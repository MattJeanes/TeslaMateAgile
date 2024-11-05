using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class FixedPriceWeeklyOptions
{
    [Required]
    public string TimeZone { get; set; }

    [Required]
    public List<string> Prices { get; set; }
}
