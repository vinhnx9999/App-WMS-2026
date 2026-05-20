using WMS.Domain.Enums;

namespace WMS.Application.Reports.DTOs;

public record DashboardDto(
    int TotalItems, int InboundThisMonth, int OutboundThisMonth,
    int LowStockAlerts, decimal TotalValue,
    List<int> WeeklyInbound, List<int> WeeklyOutbound);

public record AlertDto(
    Guid ItemId, string Sku, string Name,
    int CurrentStock, int MinQuantity,
    ItemStatus Status, string ZoneName);