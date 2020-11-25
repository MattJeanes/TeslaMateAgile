using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests
{
    public class IntegrationTests
    {
        private const string IntegrationTest = "Integration test";

        [Ignore(IntegrationTest)]
        [Test]
        public async Task IntegrationTests_Tibber()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>();

            var config = configBuilder.Build();

            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddTransient<IGraphQLJsonSerializer, SystemTextJsonSerializer>();
            services.Configure<TibberOptions>(config.GetSection("Tibber"));
            services.AddHttpClient<IPriceDataService, TibberService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<TibberOptions>>().Value;
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
            });

            var priceDataService = services.BuildServiceProvider().GetRequiredService<IPriceDataService>();
            var priceData = await priceDataService.GetPriceData(DateTimeOffset.Parse("2020-01-01T00:25:00+00:00"), DateTimeOffset.Parse("2020-01-01T15:00:00+00:00"));
        }
    }
}
