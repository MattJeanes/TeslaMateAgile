using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services;

public class BarryService : IPriceDataService
{
    private readonly HttpClient _client;
    private readonly BarryOptions _options;

    public BarryService(HttpClient client, IOptions<BarryOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
    {
        var request = new BarryRequest {
            Params = new string[] { _options.MPID, from.UtcDateTime.ToString("s")+"Z", to.UtcDateTime.ToString("s") + "Z" }.ToList()
        };
        var objAsJson = JsonSerializer.Serialize(request);
        var content = new StringContent(objAsJson, System.Text.Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync(_options.BaseUrl, content);
        resp.EnsureSuccessStatusCode();

        var barryResponse = await JsonSerializer.DeserializeAsync<BarryResponse>(await resp.Content.ReadAsStreamAsync());

        if (barryResponse == null)
        {
            throw new Exception($"Deserialization of Barry API response failed");
        }

        return barryResponse.Results.Select(x => new Price
        {
            Value = x.Value,
            ValidFrom = ParseIso8601(x.Start),
            ValidTo = ParseIso8601(x.End)
        });
    }

    public static DateTimeOffset ParseIso8601(string iso8601String)
    {
        return DateTimeOffset.ParseExact(
            iso8601String,
            new string[] { "yyyy-MM-dd'T'HH:mm:ss.FFFK" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
    }

    public class BarryPrice
    {
        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("start")]
        public string Start { get; set; }

        [JsonPropertyName("end")]
        public string End { get; set; }
    }

    public class BarryResponse
    {
        [JsonPropertyName("result")]
        public List<BarryPrice> Results { get; set; }
    }

    public class BarryRequest
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = "co.getbarry.api.v1.OpenApiController.getPrice";//"co.getbarry.api.v1.OpenApiController.getTotalKwHourlyPrice";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "0";

        [JsonPropertyName("jsonrpc")]
        public string JsonRPC { get; set; } = "2.0";


        [JsonPropertyName("params")]
        public List<string> Params { get; set; }
    }
}
