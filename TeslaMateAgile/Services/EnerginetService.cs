using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services;

public class EnerginetService : IPriceDataService
{
    private readonly HttpClient _client;
    private readonly EnerginetOptions _options;
    private readonly FixedPriceOptions _fixedPricesOptions;
    private readonly FixedPriceService _fixedPriceService;

    public EnerginetService(HttpClient client, IOptions<EnerginetOptions> options)
    {
        _client = client;
        _options = options.Value;

        _fixedPricesOptions = _options.FixedPrices;
        _fixedPriceService = new FixedPriceService(Options.Create(_fixedPricesOptions));
    }

    public async Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
    {
        var url = "Elspotprices?offset=0&start=" + from.UtcDateTime.ToString("yyyy-MM-ddTHH:mm") + "&end=" + to.UtcDateTime.ToString("yyyy-MM-ddTHH:mm") + "&filter={\"PriceArea\":[\"" + _options.Region + "\"]}&sort=HourUTC ASC&timezone=dk".Replace(@"\", string.Empty); ;
        var resp = await _client.GetAsync(url);

        resp.EnsureSuccessStatusCode();

        var prices = new List<Price>();
        var EnerginetResponse = await JsonSerializer.DeserializeAsync<EnerginetResponse>(await resp.Content.ReadAsStreamAsync());

        if (EnerginetResponse.records.Count > 0)
        {
            foreach (var record in EnerginetResponse.records)
            {
                var fixedPrices = await _fixedPriceService.GetPriceData(record.HourUTC, record.HourUTC.AddHours(1));
                var fixedPrice = fixedPrices.Sum(p => p.Value);
                
                prices.Add(new Price
                {
                    ValidFrom = record.HourUTC.AddHours(1),
                    ValidTo = record.HourUTC.AddHours(2),
                    Value = ((record.SpotPriceDKK / 1000) + fixedPrice) * _options.VAT
                });
            }
        }

        return prices;
    }

    private class EnerginetResponse
    {
        [JsonPropertyName("records")]
        public List<EnerginetResponseRow> records { get; set; }
    }

    private class EnerginetResponseRow
    {

        [JsonPropertyName("HourDK")]
        public DateTime HourUTC { get; set; }
        [JsonPropertyName("SpotPriceDKK")]
        public decimal SpotPriceDKK { get; set; }
    }
}
