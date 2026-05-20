using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.OdooIntegration.OdooInboundSync;
using WMS.Application.OdooIntegration.OdooMasterSync;
using WMS.Application.OdooIntegration.OdooOutboundSync;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;

namespace WMS.Application.BackgroundJobs;

public class OdooSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly OdooSyncConfig _config;
    private readonly ILogger<OdooSyncBackgroundService> _log;

    public OdooSyncBackgroundService(
        IServiceProvider sp,
        IOptions<OdooConfig> config,
        ILogger<OdooSyncBackgroundService> log)
    {
        _sp = sp;
        _config = config.Value.Sync;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("Odoo Sync Background Service started");

        await Task.WhenAll(
            RunLoop(SyncPickings,
                _config.PickingSyncIntervalMinutes, ct),
            RunLoop(SyncMasterData,
                _config.MasterDataIntervalMinutes, ct)
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
                _log.LogError(ex, "Odoo sync error");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(intervalMinutes), ct);
        }
    }

    private async Task SyncPickings(
        IServiceScope scope, CancellationToken ct)
    {
        var inbound = scope.ServiceProvider
            .GetRequiredService<IOdooInboundSyncService>();
        var outbound = scope.ServiceProvider
            .GetRequiredService<IOdooOutboundSyncService>();

        var inCount = await inbound.SyncInboundPickingsAsync(ct);
        var outCount = await outbound.SyncOutboundDeliveriesAsync(ct);

        _log.LogInformation(
            "Odoo picking sync: {Inbound} in, {Outbound} out",
            inCount, outCount);
    }

    private async Task SyncMasterData(
        IServiceScope scope, CancellationToken ct)
    {
        var master = scope.ServiceProvider
            .GetRequiredService<IOdooMasterSyncService>();

        var products = await master.SyncProductsAsync(ct);
        var partners = await master.SyncPartnersAsync(ct);

        _log.LogInformation(
            "Odoo master sync: {Products} products, {Partners} partners",
            products, partners);
    }
}
