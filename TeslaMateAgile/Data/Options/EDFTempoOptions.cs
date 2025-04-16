using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class EDFTempoOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public decimal BLUE_HP { get; set; }
    public decimal BLUE_HC { get; set; }
    public decimal WHITE_HP { get; set; }
    public decimal WHITE_HC { get; set; }
    public decimal RED_HP { get; set; }
    public decimal RED_HC { get; set; }
}