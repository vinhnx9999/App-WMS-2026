namespace WMS.Application.Inbound.DTOs;

public record CreateInboundItemRequest(Guid InventoryItemId, int Quantity);
