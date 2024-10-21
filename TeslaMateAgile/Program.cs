using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data.Common;
using TeslaMateAgile.Data.Enums;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Helpers.Interfaces;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile;

public class Program
{
    public async static Task Main()
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.AddJsonFile("appsettings.json");
                configBuilder.AddEnvironmentVariables();
                configBuilder.AddUserSecrets<Program>(true);
            })
            .ConfigureLogging((hostContext, loggingBuilder) =>
            {
                var config = hostContext.Configuration;
                loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                loggingBuilder.AddConsole();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                var connectionString = config.GetConnectionString("TeslaMate");
                if (string.IsNullOrEmpty(connectionString))
                {
                    string getDatabaseVariable(string variableName)
                    {
                        var option = config[variableName];
                        if (string.IsNullOrEmpty(option))
                        {
                            throw new ArgumentNullException(variableName, $"Configuration '{variableName}' or 'ConnectionStrings__TeslaMate' is required");
                        }
                        return option;
                    }
                    var databaseHost = getDatabaseVariable("DATABASE_HOST");
                    var databaseName = getDatabaseVariable("DATABASE_NAME");
                    var databaseUsername = getDatabaseVariable("DATABASE_USER");
                    var databasePassword = getDatabaseVariable("DATABASE_PASS");

                    var connectionStringBuilder = new NpgsqlConnectionStringBuilder
                    {
                        Host = databaseHost,
                        Database = databaseName,
                        Username = databaseUsername,
                        Password = databasePassword
                    };

                    var databasePortVariable = "DATABASE_PORT";
                    var databasePortStr = config[databasePortVariable];
                    if (!string.IsNullOrEmpty(databasePortStr))
                    {
                        if (int.TryParse(databasePortStr, out var databasePort))
                        {
                            connectionStringBuilder.Port = databasePort;
                        }
                        else
                        {
                            throw new ArgumentException(databasePortVariable, $"Configuration '{databasePortVariable}' is invalid, must be an integer");
                        }
                    }

                    connectionString = connectionStringBuilder.ConnectionString;
                }

                var builder = new DbConnectionStringBuilder();
                services.AddDbContext<TeslaMateDbContext>(o => o.UseNpgsql(connectionString));
                services.AddHostedService<PriceService>();
                services.AddOptions<TeslaMateOptions>()
                    .Bind(config.GetSection("TeslaMate"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddTransient<IPriceHelper, PriceHelper>();
                services.AddHttpClient();

                var energyProvider = config.GetValue("TeslaMate:EnergyProvider", EnergyProvider.Octopus);
                if (energyProvider == EnergyProvider.Octopus)
                {
                    services.AddOptions<OctopusOptions>()
                        .Bind(config.GetSection("Octopus"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddHttpClient<IDynamicPriceDataService, OctopusService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<OctopusOptions>>().Value;
                        var baseUrl = options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
                        client.BaseAddress = new Uri(baseUrl);
                    });
                }
                else if (energyProvider == EnergyProvider.Tibber)
                {
                    services.AddOptions<TibberOptions>()
                        .Bind(config.GetSection("Tibber"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddTransient<IGraphQLJsonSerializer, SystemTextJsonSerializer>();
                    services.AddHttpClient<IDynamicPriceDataService, TibberService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<TibberOptions>>().Value;
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
                    });
                }
                else if (energyProvider == EnergyProvider.FixedPrice)
                {
                    services.AddOptions<FixedPriceOptions>()
                       .Bind(config.GetSection("FixedPrice"))
                       .ValidateDataAnnotations()
                       .ValidateOnStart();
                    services.AddSingleton<IDynamicPriceDataService, FixedPriceService>();
                }
                else if (energyProvider == EnergyProvider.Awattar)
                {
                    services.AddOptions<AwattarOptions>()
                        .Bind(config.GetSection("Awattar"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddHttpClient<IDynamicPriceDataService, AwattarService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<AwattarOptions>>().Value;
                        var baseUrl = options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
                        client.BaseAddress = new Uri(baseUrl);
                    });
                }
                else if (energyProvider == EnergyProvider.Energinet)
                {
                    services.AddOptions<EnerginetOptions>()
                        .Bind(config.GetSection("Energinet"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddHttpClient<IDynamicPriceDataService, EnerginetService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<EnerginetOptions>>().Value;
                        var baseUrl = options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
                        client.BaseAddress = new Uri(baseUrl);
                    });
                }
                else if (energyProvider == EnergyProvider.HomeAssistant)
                {
                    services.AddOptions<HomeAssistantOptions>()
                        .Bind(config.GetSection("HomeAssistant"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddHttpClient<IDynamicPriceDataService, HomeAssistantService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<HomeAssistantOptions>>().Value;
                        var baseUrl = options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
                        client.BaseAddress = new Uri(baseUrl);
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
                    });
                }
                else if (energyProvider == EnergyProvider.Monta)
                {
                    services.AddOptions<MontaOptions>()
                        .Bind(config.GetSection("Monta"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    services.AddHttpClient<IWholePriceDataService, MontaService>((serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<MontaOptions>>().Value;
                        var baseUrl = options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
                        client.BaseAddress = new Uri(baseUrl);
                    });
                }
                else
                {
                    throw new ArgumentException("Invalid energy provider set", nameof(energyProvider));
                }
            });

        await builder.RunConsoleAsync();
    }
}
