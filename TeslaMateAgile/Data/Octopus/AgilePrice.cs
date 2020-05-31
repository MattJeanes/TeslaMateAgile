using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TeslaMateAgile.Data.Octopus
{
    public class AgilePrice
    {
        [JsonPropertyName("value_exc_vat")]
        public decimal ValueExcVAT { get; set; }

        [JsonPropertyName("value_inc_vat")]
        public decimal ValueIncVAT { get; set; }

        [JsonPropertyName("valid_from")]
        public DateTime ValidFrom { get; set; }

        [JsonPropertyName("valid_to")]
        public DateTime ValidTo { get; set; }
    }
}
