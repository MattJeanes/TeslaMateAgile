using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class HomeAssistantOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public string AccessToken { get; set; }

    [Required]
    public string EntityId { get; set; }
}
