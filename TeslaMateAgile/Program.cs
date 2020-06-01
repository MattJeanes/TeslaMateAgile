using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TeslaMateAgile.Data.TeslaMate;

namespace TeslaMateAgile
{
    public class Program
    {
        public static async Task Main()
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(configBuilder => {
                    configBuilder.AddEnvironmentVariables();
                    configBuilder.AddUserSecrets<Program>(true);
                    configBuilder.AddJsonFile("appsettings.json");
                })
                .ConfigureLogging((hostContext, loggingBuilder) => {
                    var config = hostContext.Configuration;
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;
                    services.AddDbContext<TeslaMateDbContext>(o => o.UseNpgsql(config.GetConnectionString("TeslaMate")));
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
