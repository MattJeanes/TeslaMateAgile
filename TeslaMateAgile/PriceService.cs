using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaMateAgile
{
    public class PriceService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly TeslaMateOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public PriceService(ILogger<PriceService> logger, IOptions<TeslaMateOptions> options, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Price service is starting");

            _timer = new Timer(async (state) =>
            {
                try
                {
                    await DoWork();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Failed to run {nameof(DoWork)}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.UpdateIntervalSeconds));

            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            _logger.LogDebug("Updating prices");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var priceHelper = scope.ServiceProvider.GetRequiredService<IPriceHelper>();
                    await priceHelper.Update();
                }
                _logger.LogDebug("Price update complete");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update prices");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Price service is stopping");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}