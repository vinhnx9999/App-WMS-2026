using WMS.Infrastructure.ERPs.SAP.MasterSync;

namespace WMS.Application.SAPIntegration.SapMasterSync;

public interface ISapMasterSyncService
{
    Task<SapStockItem> GetSapStockAsync(string material, CancellationToken ct);
    Task<IEnumerable<SyncHistoryItem>> GetSyncHistoryAsync(CancellationToken ct);
    Task<string?> SyncDeliveriesAsync(CancellationToken ct);
    Task<int> SyncMaterialsAsync(CancellationToken ct);
}
