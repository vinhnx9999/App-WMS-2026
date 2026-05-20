using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

namespace WMS.Application.SAPIntegration.SapInboundSync;

public interface ISapInboundSyncService
{
    Task<int> SyncPurchaseOrdersAsync(CancellationToken ct = default);
    Task<SapGrResult> PostGoodsReceiptAsync(
        Guid inboundOrderId, CancellationToken ct = default);
}
