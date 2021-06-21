using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TeslaMateAgile.Data;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services
{
    public class AwattarService : IPriceDataService
    {
        private readonly HttpClient _client;

        public AwattarService(HttpClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
        {
            var url = $"marketdata?start={from.UtcDateTime.AddHours(-1):o}&end={to.UtcDateTime.AddHours(1):o}";
            var resp = await _client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var agileResponse = await JsonSerializer.DeserializeAsync<AwattarResponse>(await resp.Content.ReadAsStreamAsync());
            if (agileResponse.Results.Any(x => x.Unit != "Eur/MWh"))
            {
                throw new Exception($"Unknown price unit(s) detected from aWATTar API: {string.Join(", ", agileResponse.Results.Select(x => x.Unit).Distinct())}");
            }
            return agileResponse.Results.Select(x => new Price
            {
                Value = x.MarketPrice / 1000,
                ValidFrom = DateTimeOffset.FromUnixTimeSeconds(x.StartTimestamp / 1000),
                ValidTo = DateTimeOffset.FromUnixTimeSeconds(x.EndTimestamp / 1000)
            });
        }

        public class AwattarPrice
        {
            [JsonPropertyName("marketprice")]
            public decimal MarketPrice { get; set; }

            [JsonPropertyName("unit")]
            public string Unit { get; set; }

            [JsonPropertyName("start_timestamp")]
            public long StartTimestamp { get; set; }

            [JsonPropertyName("end_timestamp")]
            public long EndTimestamp { get; set; }
        }

        public class AwattarResponse
        {
            [JsonPropertyName("data")]
            public List<AwattarPrice> Results { get; set; }
        }
    }
}
