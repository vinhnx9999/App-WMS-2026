namespace WMS.Application.Inbound.DTOs;

public record CompleteQcRequest(
    List<CompleteQcItemRequest> Items);

public record CompleteQcItemRequest(
    Guid SkuId,
    int PassedQuantity,
    int FailedQuantity,
    string? Notes);
