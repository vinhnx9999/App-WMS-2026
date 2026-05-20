namespace WMS.Application.OdooIntegration.OdooMasterSync;

public interface IOdooMasterSyncService
{
    Task<int> SyncProductsAsync(CancellationToken ct = default);
    Task<int> SyncPartnersAsync(CancellationToken ct = default);
    Task<decimal> GetOdooStockAsync(string sku, CancellationToken ct = default);
}
