using WMS.Domain.Enums;

namespace WMS.Application.Inbound.DTOs;

public record InboundReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid? InboundOrderId,
    string? InboundOrderNumber,
    Guid WarehouseId,
    string? WarehouseName,
    ReceiptStatus Status,
    DateTime CreatedAt,
    int ItemsCount
);
