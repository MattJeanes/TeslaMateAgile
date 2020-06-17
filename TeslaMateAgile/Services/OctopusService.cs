using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Octopus;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services
{
    public class OctopusService : IOctopusService
    {
        private readonly OctopusOptions _options;
        private readonly HttpClient _client;

        public OctopusService(HttpClient client, IOptions<OctopusOptions> options)
        {
            _options = options.Value;
            _client = client;
            var baseUrl = _options.BaseUrl;
            if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
            _client.BaseAddress = new Uri(baseUrl);
        }

        public async Task<IOrderedEnumerable<AgilePrice>> GetAgilePrices(DateTime from, DateTime to)
        {
            var url = $"products/{_options.ProductCode}/electricity-tariffs/{_options.TariffCode}-{_options.RegionCode}/standard-unit-rates?period_from={from:o}&period_to={to:o}";
            var list = new List<AgilePrice>();
            do
            {
                var resp = await _client.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                var agileResponse = await JsonSerializer.DeserializeAsync<AgileResponse>(await resp.Content.ReadAsStreamAsync());
                list.AddRange(agileResponse.Results);
                url = agileResponse.Next;
                if (string.IsNullOrEmpty(url))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(5000); // back off API so they don't ban us
                }
            }
            while (true);
            return list.OrderBy(x => x.ValidFrom);
        }
    }
}
