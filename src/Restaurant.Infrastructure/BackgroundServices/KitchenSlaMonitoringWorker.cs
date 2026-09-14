using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Restaurant.Application.Interfaces;

namespace Restaurant.Infrastructure.BackgroundServices;

public class KitchenSlaMonitoringWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KitchenSlaMonitoringWorker> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public KitchenSlaMonitoringWorker(IServiceProvider serviceProvider, ILogger<KitchenSlaMonitoringWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kitchen SLA Monitoring Worker đã khởi động.");

        using var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kitchenService = scope.ServiceProvider.GetRequiredService<IKitchenService>();
                await kitchenService.CheckAndBroadcastSlaWarningsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong khi quét SLA trễ món bếp.");
            }
        }
    }
}
