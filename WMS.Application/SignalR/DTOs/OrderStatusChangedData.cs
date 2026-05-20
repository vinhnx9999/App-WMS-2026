namespace WMS.Application.SignalR.DTOs;

public record OrderStatusChangedData
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public string OldStatus { get; init; } = "";
    public string NewStatus { get; init; } = "";
    public string? PartnerName { get; init; }
    public int ItemsCount { get; init; }
}
