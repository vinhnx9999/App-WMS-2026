namespace WMS.Application.Inbound.DTOs;

public record InboundItemDto(
    string Sku, string ItemName,
    int Quantity, int ReceivedQuantity,
    Guid? SupplierId, string? SupplierName);
