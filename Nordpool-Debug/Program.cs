// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Text.Json.Serialization;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;


static async Task<string> GetPriceData(DateTimeOffset from, DateTimeOffset to, NordpoolOptions _options, HttpClient _client)
{
    var url = _options.BaseUrl + "?currency=," + _options.Currency + "&endDate=" + to.UtcDateTime.ToString("dd-MM-yyyy");

    _client = new HttpClient();
    try
    {
        var resp = await _client.GetAsync(url);

        resp.EnsureSuccessStatusCode();

        List<Price> prices = new List<Price>();

        var nordPoolResponse = await JsonSerializer.DeserializeAsync<NordpoolResponse>(await resp.Content.ReadAsStreamAsync());

        if(nordPoolResponse.data.Rows.Count > 0)
        {
            foreach (NordpoolResponseRow row in nordPoolResponse.data.Rows)
            {
                if(row.IsExtraRow)
                {
                    continue;
                }

                foreach (NordpoolResponseRowColumn column in row.Columns) {
                    decimal value;                    
                    Decimal.TryParse(column.Value.Replace(',', '.'), out value);
                    var area = column.Name;
                    if (_options.Region == area)
                    {
                        //values.push({ area: area, date: date.toISOString(), value: value })
                        Console.WriteLine("Start: " + row.StartTime + "; End: " + row.EndTime + "; Value: " + value/1000 + "; Region: " + area);
                        prices.Add(new Price
                        {
                            ValidFrom = DateTimeOffset.Parse(row.StartTime),
                            ValidTo = DateTimeOffset.Parse(row.EndTime),
                            Value = value/1000
                        });
                    }
                }
            }
        }

        Console.WriteLine("DONE");

        return "OK";
    } catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return ex.Message;
    }


}

/*
      for (const column of row.Columns) {
        const value = parseFloat(column.Value.replace(/,/, '.').replace(/ /g, ''))
        if (isNaN(value)) {
          continue
        }
        const area = column.Name
        if (!opts.area || opts.area.indexOf(area) >= 0) {
          values.push({ area: area, date: date.toISOString(), value: value })
        }
      }
    }
    return values
  }
*/


MainAsync().Wait();

static async Task MainAsync()
{
    try
    {
        Console.WriteLine("Hello, World!");
        var testOptions = new NordpoolOptions();
        testOptions.BaseUrl = "http://www.nordpoolspot.com/api/marketdata/page/10";
        testOptions.Currency = NordpoolCurrency.DKK;
        testOptions.Region = "DK1";


        var test = new NordpoolService(new HttpClient(), testOptions);

        var test2 = await GetPriceData(new DateTimeOffset(2022, 2, 20, 0, 0, 0, new TimeSpan(1, 0, 0)), new DateTimeOffset(2022, 2, 20, 23, 59, 0, new TimeSpan(1, 0, 0)), testOptions, new HttpClient());

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
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
    public string StartTime { get; set; }

    [JsonPropertyName("EndTime")]
    public string EndTime { get; set; }

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
//test.GetPriceData(new DateTimeOffset(2022, 2, 20, 0, 0, 0, new TimeSpan(1, 0, 0)), new DateTimeOffset(2022, 2, 20, 23, 59, 0, new TimeSpan(1, 0, 0)));