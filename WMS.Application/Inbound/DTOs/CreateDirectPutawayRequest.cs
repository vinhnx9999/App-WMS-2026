namespace WMS.Application.Inbound.DTOs;

public record CreateDirectPutawayRequest(
    Guid TenantId,
    Guid WarehouseId,
    List<CreateDirectPutawayItemRequest> Items);

public record CreateDirectPutawayItemRequest(
    Guid SkuId,
    int Quantity,
    Guid TargetLocationId,
    string? PalletCode = null,
    Guid? SupplierId = null,
    DateTime? ExpiryDate = null,
    string? SerialNumber = null,
    string? LotNumber = null);
