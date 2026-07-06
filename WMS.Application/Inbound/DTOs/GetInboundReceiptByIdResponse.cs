using WMS.Domain.Enums;

namespace WMS.Application.Inbound.DTOs;

public sealed record GetInboundReceiptByIdResponse(
    Guid Id,
    string ReceiptNumber,
    Guid? InboundOrderId,
    string? InboundOrderNumber,
    Guid WarehouseId,
    string? WarehouseName,
    ReceiptStatus Status,
    DateTime CreatedAt,
    List<InboundReceiptItemDetailDto> Items
);

public sealed record InboundReceiptItemDetailDto(
    Guid SkuId,
    string? SkuCode,
    string? SkuName,
    int ExpectedQuantity,
    int ReceivedQuantity,
    string? Notes,
    Guid? SupplierId,
    string? SupplierName,
    DateOnly? ExpiryDate,
    string? SerialNumber,
    string? LotNumber
);
