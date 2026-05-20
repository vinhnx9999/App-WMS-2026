namespace WMS.Application.SignalR.DTOs;

// ─── Alerts ───

public record StockAlertData
{
    public Guid ItemId { get; init; }
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public int CurrentQuantity { get; init; }
    public int MinQuantity { get; init; }
    public string? ZoneName { get; init; }
}
