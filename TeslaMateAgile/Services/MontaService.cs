using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services
{
    public class MontaService : IWholePriceDataService
    {
        private readonly HttpClient _client;
        private readonly MontaOptions _options;

        public MontaService(HttpClient client, IOptions<MontaOptions> options)
        {
            _client = client;
            _options = options.Value;
        }

        public async Task<decimal> GetTotalPrice(DateTimeOffset from, DateTimeOffset to)
        {
            var accessToken = await GetAccessToken();
            var charges = await GetCharges(accessToken, from, to);
            var totalPrice = CalculateTotalPrice(charges);
            return totalPrice;
        }

        private async Task<string> GetAccessToken()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/auth/token");
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                client_id = _options.ClientId,
                client_secret = _options.ClientSecret,
                grant_type = "client_credentials"
            }), System.Text.Encoding.UTF8, "application/json");
            request.Content = content;

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);

            return tokenResponse.access_token;
        }

        private async Task<Charge[]> GetCharges(string accessToken, DateTimeOffset from, DateTimeOffset to)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/charges?from={from:o}&to={to:o}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var chargesResponse = JsonSerializer.Deserialize<ChargesResponse>(responseBody);

            return chargesResponse.data;
        }

        private decimal CalculateTotalPrice(Charge[] charges)
        {
            decimal totalPrice = 0;
            foreach (var charge in charges)
            {
                totalPrice += charge.cost;
            }
            return totalPrice;
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
        }

        private class ChargesResponse
        {
            public Charge[] data { get; set; }
        }

        private class Charge
        {
            public decimal cost { get; set; }
        }
    }
}
