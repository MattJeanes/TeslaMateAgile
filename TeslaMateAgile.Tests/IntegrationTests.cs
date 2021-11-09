using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests;

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

        var from = DateTimeOffset.Parse("2020-01-01T00:25:00+00:00");
        var to = DateTimeOffset.Parse("2020-01-01T15:00:00+00:00");

        var priceData = await priceDataService.GetPriceData(from, to);

        Assert.LessOrEqual(priceData.Min(x => x.ValidFrom), from);
        Assert.GreaterOrEqual(priceData.Max(x => x.ValidTo), to);
    }

    [Ignore(IntegrationTest)]
    [Test]
    public async Task IntegrationTests_Awattar()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>();

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddOptions<AwattarOptions>()
                        .Bind(config.GetSection("Awattar"))
                        .ValidateDataAnnotations();
        services.AddHttpClient<IPriceDataService, AwattarService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AwattarOptions>>().Value;
            var baseUrl = options.BaseUrl;
            if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
            client.BaseAddress = new Uri(baseUrl);
        });

        var priceDataService = services.BuildServiceProvider().GetRequiredService<IPriceDataService>();

        var from = DateTimeOffset.Parse("2020-01-01T00:25:00+00:00");
        var to = DateTimeOffset.Parse("2020-01-01T15:55:00+00:00");

        var priceData = await priceDataService.GetPriceData(from, to);

        Assert.LessOrEqual(priceData.Min(x => x.ValidFrom), from);
        Assert.GreaterOrEqual(priceData.Max(x => x.ValidTo), to);
    }
}
