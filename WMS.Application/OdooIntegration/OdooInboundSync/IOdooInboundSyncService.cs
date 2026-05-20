namespace WMS.Application.OdooIntegration.OdooInboundSync;

public interface IOdooInboundSyncService
{
    Task<int> SyncInboundPickingsAsync(CancellationToken ct = default);
    Task ConfirmReceiptAsync(Guid wmsOrderId,
        CancellationToken ct = default);
}
