namespace WMS.Application.Outbound.DTOs;

public record CreateOutboundItemRequest(Guid InventoryItemId, int Quantity);
