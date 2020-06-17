using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeslaMateAgile.Data.Octopus
{
    public class AgileResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string Next { get; set; }

        [JsonPropertyName("previous")]
        public string Previous { get; set; }

        [JsonPropertyName("results")]
        public List<AgilePrice> Results { get; set; }
    }
}
