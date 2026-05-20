using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.SAPIntegration.SapInboundSync;
using WMS.Application.SAPIntegration.SapMasterSync;
using WMS.Infrastructure.ERPs.SAP.DataConfig;

namespace WMS.Application.BackgroundJobs;

/// <summary>Background service chạy periodic sync với SAP</summary>
public class SapSyncBackgroundService(
    IServiceProvider sp,
    IOptions<SapConfig> config,
    ILogger<SapSyncBackgroundService> log) : BackgroundService
{
    private readonly IServiceProvider _sp = sp;
    private readonly SapSyncConfig _config = config.Value.Sync;
    private readonly ILogger<SapSyncBackgroundService> _log = log;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _log.LogInformation("SAP Sync Background Service started");

        // Run multiple sync loops in parallel
        await Task.WhenAll(
            RunLoop(SyncPurchaseOrders,
                _config.PurchaseOrderSyncIntervalMinutes, stoppingToken),
            RunLoop(SyncDeliveries,
                _config.DeliverySyncIntervalMinutes, stoppingToken),
            RunLoop(SyncMasterData,
                _config.MasterDataIntervalMinutes, stoppingToken)
        );
    }

    private async Task RunLoop(
        Func<IServiceScope, CancellationToken, Task> action,
        int intervalMinutes, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                await action(scope, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SAP sync error");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(intervalMinutes), ct);
        }
    }

    private async Task SyncPurchaseOrders(
        IServiceScope scope, CancellationToken ct)
    {
        var svc = scope.ServiceProvider
            .GetRequiredService<ISapInboundSyncService>();
        var count = await svc.SyncPurchaseOrdersAsync(ct);
        _log.LogInformation("Scheduled PO sync: {Count} orders", count);
    }

    private async Task SyncDeliveries(
        IServiceScope scope, CancellationToken ct)
    {
        var svc = scope.ServiceProvider
            .GetRequiredService<ISapMasterSyncService>();
        var count = await svc.SyncDeliveriesAsync(ct);
        _log.LogInformation("Scheduled Delivery sync: {Count}", count);
    }

    private async Task SyncMasterData(
        IServiceScope scope, CancellationToken ct)
    {
        var svc = scope.ServiceProvider
            .GetRequiredService<ISapMasterSyncService>();
        var count = await svc.SyncMaterialsAsync(ct);
        _log.LogInformation("Scheduled Material sync: {Count}", count);
    }
}
