using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services;

public class NordpoolService : IPriceDataService
{
    private readonly HttpClient _client;
    private readonly NordpoolOptions _options;

    public NordpoolService(HttpClient client, IOptions<NordpoolOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
    {
        var url = "?currency=," + _options.Currency + "&endDate=" + to.UtcDateTime.ToString("dd-MM-yyyy");

        var resp = await _client.GetAsync(url);

        resp.EnsureSuccessStatusCode();

        var prices = new List<Price>();

        var nordPoolResponse = await JsonSerializer.DeserializeAsync<NordpoolResponse>(await resp.Content.ReadAsStreamAsync());

        if (nordPoolResponse.data.Rows.Count > 0)
        {
            foreach (var row in nordPoolResponse.data.Rows)
            {
                if (row.IsExtraRow)
                {
                    continue;
                }

                foreach (var column in row.Columns)
                {
                    decimal.TryParse(column.Value.Replace(',', '.'), out var value);

                    var area = column.Name;

                    if (_options.Region == area
                        && row.StartTime >= from.AddHours(-1)
                        && row.EndTime < to.AddHours(1)
                        && !prices.Any(x => x.ValidFrom == row.StartTime)
                    )
                    {
                        prices.Add(new Price
                        {
                            ValidFrom = row.StartTime,
                            ValidTo = row.EndTime,
                            Value = (value / 1000) * _options.VAT
                        });
                    }
                }
            }
        }

        return prices;

    }

    private class NordpoolResponse
    {
        [JsonPropertyName("data")]
        public NordpoolReponseRows data { get; set; }
    }

    private class NordpoolReponseRows
    {
        [JsonPropertyName("Rows")]
        public List<NordpoolResponseRow> Rows { get; set; }
    }

    private class NordpoolResponseRow
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("StartTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("EndTime")]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("IsExtraRow")]
        public bool IsExtraRow { get; set; }

        [JsonPropertyName("IsNtcRow")]
        public bool IsNtcRow { get; set; }

        [JsonPropertyName("Columns")]
        public List<NordpoolResponseRowColumn> Columns { get; set; }
    }

    private class NordpoolResponseRowColumn
    {
        [JsonPropertyName("Index")]
        public int Index { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Value")]
        public string Value { get; set; }
    }
}
