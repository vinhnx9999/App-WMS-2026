namespace WMS.Application.Inbound.DTOs;

public record CompletePutawayRequest(
    List<CompletePutawayItemRequest> Items);

public record CompletePutawayItemRequest(
    Guid SkuId,
    Guid ActualLocationId);
