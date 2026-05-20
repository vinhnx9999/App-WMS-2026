namespace WMS.Application.SignalR.DTOs;

// ─── Dashboard Summary ───

public record DashboardSummaryData
{
    public int TotalItems { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
    public int PendingInbound { get; init; }
    public int PendingOutbound { get; init; }
    public int TodayInbound { get; init; }
    public int TodayOutbound { get; init; }
    public decimal TotalValue { get; init; }
    public int ActiveUsers { get; init; }
    public int ConnectedClients { get; init; }
}