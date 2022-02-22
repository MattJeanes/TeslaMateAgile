using Microsoft.Extensions.Options;
using System.Globalization;
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
        var url = _options.BaseUrl + "?currency=," + _options.Currency + "&endDate=" + to.UtcDateTime.ToString("dd-MM-yyyy");

        var resp = await _client.GetAsync(url);

        resp.EnsureSuccessStatusCode();

        List<Price> prices = new List<Price>();

        var nordPoolResponse = await JsonSerializer.DeserializeAsync<NordpoolResponse>(await resp.Content.ReadAsStreamAsync());

        if (nordPoolResponse.data.Rows.Count > 0)
        {
            foreach (NordpoolResponseRow row in nordPoolResponse.data.Rows)
            {
                if (row.IsExtraRow)
                {
                    continue;
                }

                foreach (NordpoolResponseRowColumn column in row.Columns)
                {
                    decimal value;
                    Decimal.TryParse(column.Value.Replace(',', '.'), out value);

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
                            Value = (value / 1000)
                        });
                    }
                }
            }
        }

        return prices;

    }

    public class NordpoolResponse
    {
        [JsonPropertyName("data")]
        public NordpoolReponseRows data { get; set; }
    }

    public class NordpoolReponseRows
    {
        [JsonPropertyName("Rows")]
        public List<NordpoolResponseRow> Rows { get; set; }
    }

    public class NordpoolResponseRow
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

    public class NordpoolResponseRowColumn
    {
        [JsonPropertyName("Index")]
        public int Index { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Value")]
        public string Value { get; set; }
    }
}
