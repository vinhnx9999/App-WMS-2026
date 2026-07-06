using WMS.Domain.Enums;

namespace WMS.Application.Inbound.DTOs;

public sealed record GetInboundByIdResponse(
    Guid Id,
    string OrderNumber,
    DateOnly? ExpectedDate,
    DateOnly? ReceivedDate,
    InboundStatus Status,
    decimal TotalValue,
    string? Notes,
    List<InboundItemDetailDto> Items
);

public sealed record InboundItemDetailDto(
    Guid SkuId,
    string? SkuCode,
    string? SkuName,
    int Quantity,
    int ReceivedQuantity,
    Guid? SupplierId,
    string? SupplierName,
    DateOnly? ExpiryDate,
    string? SerialNumber,
    string? LotNumber,
    string? Note
);
