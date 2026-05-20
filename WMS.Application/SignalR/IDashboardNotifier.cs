using WMS.Application.SignalR.DTOs;

namespace WMS.Application.SignalR;

public interface IDashboardNotifier
{
    // ─── Broadcast to all dashboard clients ───
    Task InventoryChangedAsync(InventoryChangedData data);
    Task InboundStatusChangedAsync(OrderStatusChangedData data);
    Task OutboundStatusChangedAsync(OrderStatusChangedData data);
    Task LowStockAlertAsync(StockAlertData data);
    Task OutOfStockAlertAsync(StockAlertData data);
    Task DashboardSummaryChangedAsync(DashboardSummaryData data);

    // ─── Zone-specific group ───
    Task ZoneUtilizationChangedAsync(ZoneUtilizationData data);

    // ─── Connection stats ───
    Task<int> GetConnectedClientsAsync();
}
