using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using TeslaMateAgile.Data.Enums;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Data.TeslaMate;
using TeslaMateAgile.Helpers.Interfaces;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile
{
    public class Program
    {
        public static async Task Main()
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddEnvironmentVariables();
                    configBuilder.AddUserSecrets<Program>(true);
                    configBuilder.AddJsonFile("appsettings.json");
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

                        connectionString = connectionStringBuilder.ConnectionString;
                    }

                    var builder = new DbConnectionStringBuilder();
                    services.AddDbContext<TeslaMateDbContext>(o => o.UseNpgsql(connectionString));
                    services.AddHostedService<PriceService>();
                    services.AddOptions<TeslaMateOptions>()
                        .Bind(config.GetSection("TeslaMate"))
                        .ValidateDataAnnotations();
                    services.AddTransient<IPriceHelper, PriceHelper>();
                    services.AddHttpClient();

                    var energyProvider = config.GetValue("EnergyProvider", EnergyProvider.Octopus);
                    if (energyProvider == EnergyProvider.Octopus)
                    {
                        services.AddOptions<OctopusOptions>()
                            .Bind(config.GetSection("Octopus"))
                            .ValidateDataAnnotations();
                        services.AddHttpClient<IPriceDataService, OctopusService>((serviceProvider, client) =>
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
                            .ValidateDataAnnotations();
                        services.AddTransient<IGraphQLJsonSerializer, SystemTextJsonSerializer>();
                        services.AddHttpClient<IPriceDataService, TibberService>((serviceProvider, client) =>
                        {
                            var options = serviceProvider.GetRequiredService<IOptions<TibberOptions>>().Value;
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
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
}
