using WMS.Domain.Enums;

namespace WMS.Application.Outbound.DTOs;

public record OutboundOrderDto(
    Guid Id, string ShipmentNumber, string PartnerName,
    string? Destination, DateOnly? ExpectedDelivery,
    OutboundStatus Status, decimal TotalValue,
    int ItemsCount, List<OutboundItemDto> Items,
    DateTime CreatedAt);
