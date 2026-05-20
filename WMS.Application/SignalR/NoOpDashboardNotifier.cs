using WMS.Application.SignalR.DTOs;

namespace WMS.Application.SignalR;

/// <summary>
/// No-op fallback — dùng khi SignalR chưa config.
/// Services vẫn gọi notifier mà không crash.
/// </summary>
public class NoOpDashboardNotifier : IDashboardNotifier
{
    private static Task Done => Task.CompletedTask;
    public Task InventoryChangedAsync(InventoryChangedData d) => Done;
    public Task InboundStatusChangedAsync(OrderStatusChangedData d) => Done;
    public Task OutboundStatusChangedAsync(OrderStatusChangedData d) => Done;
    public Task LowStockAlertAsync(StockAlertData d) => Done;
    public Task OutOfStockAlertAsync(StockAlertData d) => Done;
    public Task ZoneUtilizationChangedAsync(ZoneUtilizationData d) => Done;
    public Task DashboardSummaryChangedAsync(DashboardSummaryData d) => Done;
    public Task<int> GetConnectedClientsAsync() => Task.FromResult(0);
}