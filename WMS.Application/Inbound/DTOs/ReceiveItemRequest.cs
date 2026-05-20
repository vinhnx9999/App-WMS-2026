namespace WMS.Application.Inbound.DTOs;

public record ReceiveItemRequest(
    Guid InventoryItemId, int ReceivedQuantity, string? Note);