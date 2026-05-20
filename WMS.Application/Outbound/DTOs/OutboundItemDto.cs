namespace WMS.Application.Outbound.DTOs;

public record OutboundItemDto(
    Guid InventoryItemId, string Sku, string ItemName,
    int Quantity, int PickedQuantity, string? Note);
