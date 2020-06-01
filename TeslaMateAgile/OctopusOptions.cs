using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile
{
    public class OctopusOptions
    {
        [Required]
        public string AgileUrl { get; set; }
    }
}