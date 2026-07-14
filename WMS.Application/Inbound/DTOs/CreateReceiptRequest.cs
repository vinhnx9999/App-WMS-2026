namespace WMS.Application.Inbound.DTOs;

public record CreateReceiptRequest(
    Guid? InboundOrderId,
    Guid WarehouseId,
    List<CreateReceiptItemRequest> Items);

public record CreateReceiptItemRequest(
    Guid SkuId,
    int ExpectedQuantity,
    int ReceivedQuantity,
    string? Notes,
    Guid? SupplierId = null,
    DateOnly? ExpiryDate = null,
    string? SerialNumber = null,
    string? LotNumber = null);
