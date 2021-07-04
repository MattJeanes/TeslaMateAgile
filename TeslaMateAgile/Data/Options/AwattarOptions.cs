using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options
{
    public class AwattarOptions
    {
        [Required]
        public string BaseUrl { get; set; }
        public decimal VATMultiplier { get; set; }
    }
}