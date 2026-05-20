using WMS.Domain.Enums;

namespace WMS.Application.Inventory.DTOs;

public record InventoryDto(
    Guid Id, string Sku, string Name, string? Description,
    string? CategoryName, Guid? CategoryId,
    string? ZoneName, Guid? ZoneId,
    string? Location, int Quantity, int MinQuantity,
    decimal UnitPrice, ItemStatus Status, DateTime UpdatedAt);

