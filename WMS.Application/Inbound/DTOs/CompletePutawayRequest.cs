namespace WMS.Application.Inbound.DTOs;

public record CompletePutawayRequest(
    List<CompletePutawayItemRequest> Items);

public record CompletePutawayItemRequest(
    Guid SkuId,
    Guid ActualLocationId,
    string? PalletCode = null,
    Guid? SupplierId = null,
    DateTime? ExpiryDate = null,
    string? SerialNumber = null,
    string? LotNumber = null);
