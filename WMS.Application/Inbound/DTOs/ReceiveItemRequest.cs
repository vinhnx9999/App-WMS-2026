namespace WMS.Application.Inbound.DTOs;

public record ReceiveItemRequest(
    Guid SkuId, int ReceivedQuantity, string? Note);