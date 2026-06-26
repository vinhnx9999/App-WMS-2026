namespace WMS.Application.Inbound.DTOs;

public record CreateReceiptRequest(
    Guid? InboundOrderId,
    Guid WarehouseId,
    List<CreateReceiptItemRequest> Items);

public record CreateReceiptItemRequest(
    Guid SkuId,
    int ExpectedQuantity,
    int ReceivedQuantity,
    string? Notes);
