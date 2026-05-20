using WMS.Infrastructure.ERPs.SAP.MasterSync;

namespace WMS.Application.SAPIntegration.SapMasterSync;

public class SapMasterSyncService : ISapMasterSyncService
{
    public Task<SapStockItem> GetSapStockAsync(string material, CancellationToken ct)
    {
        return Task.FromResult<SapStockItem>(new SapStockItem
        {
            Material = material,
            Stock = 100
        });
    }

    public Task<IEnumerable<SyncHistoryItem>> GetSyncHistoryAsync(CancellationToken ct)
    {
        return Task.FromResult<IEnumerable<SyncHistoryItem>>(
        [
            new() {
                Id = Guid.NewGuid(),
                EntityType = "Material",
                SyncedAt = DateTime.UtcNow.AddDays(-1),
                Status = "Success",
                Details = "Synced 100 materials"
            },
            new() {
                Id = Guid.NewGuid(),
                EntityType = "Delivery",
                SyncedAt = DateTime.UtcNow.AddDays(-2),
                Status = "Failed",
                Details = "Connection timeout"
            }
        ]);
    }

    public async Task<string?> SyncDeliveriesAsync(CancellationToken ct)
    {
        await Task.Delay(50, ct);

        //TODO later
        Console.Write("Syncing deliveries from SAP...");
        return "";
    }

    public Task<int> SyncMaterialsAsync(CancellationToken ct)
    {
        return Task.FromResult(1);
    }
}
