namespace WMS.Application.Inventory.DTOs;

public record UpdateInventoryRequest(
    Guid? SkuId, 
    Guid? LocationId, 
    Guid? SupplierId,
    string? SerialNumber,
    Guid? PalletId,
    int? Quantity, 
    decimal? UnitPrice,
    DateTime? PutawayDate,
    DateTime? ExpiryDate);