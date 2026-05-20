using WMS.Domain.Enums;

namespace WMS.Application.SignalR.DTOs;

public record InventoryChangedData
{
    public Guid ItemId { get; init; }
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public int OldQuantity { get; init; }
    public int NewQuantity { get; init; }
    public ItemStatus OldStatus { get; init; }
    public ItemStatus NewStatus { get; init; }
    public Guid? ZoneId { get; init; }
    public string? ZoneName { get; init; }
    public string ChangeType { get; init; } = ""; // "inbound" | "outbound" | "adjust"
}