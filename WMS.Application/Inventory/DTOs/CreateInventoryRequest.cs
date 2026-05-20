namespace WMS.Application.Inventory.DTOs;

public record CreateInventoryRequest(
    string Sku, string Name, string? Description,
    Guid? CategoryId, Guid? ZoneId, string? Location,
    int Quantity, int MinQuantity, decimal UnitPrice, string? Barcode);

