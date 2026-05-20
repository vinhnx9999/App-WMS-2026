using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WMS.Application.SignalR.DTOs;

namespace WMS.Application.SignalR;

public class DashboardNotifier(
    IHubContext<DashboardHub> hub,
    ILogger<DashboardNotifier> log) : IDashboardNotifier
{
    private readonly IHubContext<DashboardHub> _hub = hub;
    private readonly ILogger<DashboardNotifier> _log = log;

    // Group names
    private const string DashboardGroup = "dashboard";

    // ═══ Broadcast to all dashboard clients ═══

    public Task InventoryChangedAsync(InventoryChangedData data)
    {
        _log.LogDebug("SignalR → inventory.updated: {Sku} {Old}→{New}",
            data.Sku, data.OldQuantity, data.NewQuantity);

        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "inventory.updated",
            new DashboardEvent<InventoryChangedData>
            {
                Event = "inventory.updated",
                Data = data,
            });
    }

    public Task InboundStatusChangedAsync(OrderStatusChangedData data)
    {
        _log.LogDebug("SignalR → inbound.statusChanged: {Num} {Old}→{New}",
            data.OrderNumber, data.OldStatus, data.NewStatus);

        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "inbound.statusChanged",
            new DashboardEvent<OrderStatusChangedData>
            {
                Event = "inbound.statusChanged",
                Data = data,
            });
    }

    public Task OutboundStatusChangedAsync(OrderStatusChangedData data)
    {
        _log.LogDebug("SignalR → outbound.statusChanged: {Num} {Old}→{New}",
            data.OrderNumber, data.OldStatus, data.NewStatus);

        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "outbound.statusChanged",
            new DashboardEvent<OrderStatusChangedData>
            {
                Event = "outbound.statusChanged",
                Data = data,
            });
    }

    public Task LowStockAlertAsync(StockAlertData data)
    {
        _log.LogWarning("SignalR → alert.lowStock: {Sku} qty={Qty}",
            data.Sku, data.CurrentQuantity);

        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "alert.lowStock",
            new DashboardEvent<StockAlertData>
            {
                Event = "alert.lowStock",
                Data = data,
            });
    }

    public Task OutOfStockAlertAsync(StockAlertData data)
    {
        _log.LogWarning("SignalR → alert.outOfStock: {Sku}", data.Sku);

        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "alert.outOfStock",
            new DashboardEvent<StockAlertData>
            {
                Event = "alert.outOfStock",
                Data = data,
            });
    }

    public Task ZoneUtilizationChangedAsync(ZoneUtilizationData data)
    {
        _log.LogDebug("SignalR → zone.utilization: {Name} {Pct:F1}%",
            data.Name, data.UtilizationPct);

        // Broadcast to dashboard group
        var dashboardSend = _hub.Clients.Group(DashboardGroup).SendAsync(
            "zone.utilization",
            new DashboardEvent<ZoneUtilizationData>
            {
                Event = "zone.utilization",
                Data = data,
            });

        // Also send to zone-specific group
        var zoneGroup = $"zone:{data.ZoneId}";
        var zoneSend = _hub.Clients.Group(zoneGroup).SendAsync(
            "zone.utilization",
            new DashboardEvent<ZoneUtilizationData>
            {
                Event = "zone.utilization",
                Data = data,
            });

        return Task.WhenAll(dashboardSend, zoneSend);
    }

    public Task DashboardSummaryChangedAsync(DashboardSummaryData data)
    {
        return _hub.Clients.Group(DashboardGroup).SendAsync(
            "dashboard.summary",
            new DashboardEvent<DashboardSummaryData>
            {
                Event = "dashboard.summary",
                Data = data,
            });
    }

    public Task<int> GetConnectedClientsAsync() =>
        Task.FromResult(DashboardHub.ConnectedCount);
}
