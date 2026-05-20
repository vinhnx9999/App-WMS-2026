namespace WMS.Application.Inventory.DTOs;

public record UpdateInventoryRequest(
    string? Name, string? Description,
    Guid? SkuId, Guid? LocationId, string? Location,
    int? Quantity, int? MinQuantity, decimal? UnitPrice);