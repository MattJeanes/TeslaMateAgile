using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using TeslaMateAgile.Data.TeslaMate;

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
                        Func<string, string> getDatabaseVariable = (string variableName) =>
                        {
                            var option = config[variableName];
                            if (string.IsNullOrEmpty(option))
                            {
                                throw new ArgumentNullException(variableName, $"Configuration '{variableName}' or 'ConnectionStrings__TeslaMate' is required");
                            }
                            return option;
                        };
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
                    services.AddOptions<OctopusOptions>()
                        .Bind(config.GetSection("Octopus"))
                        .ValidateDataAnnotations();
                    services.AddOptions<TeslaMateOptions>()
                        .Bind(config.GetSection("TeslaMate"))
                        .ValidateDataAnnotations();
                    services.AddTransient<IPriceHelper, PriceHelper>();
                    services.AddHttpClient();
                });

            await builder.RunConsoleAsync();
        }
    }
}
