namespace WMS.Application.Outbound.DTOs;

public record ShipItemRequest(Guid InventoryItemId, int PickedQuantity);