using WMS.Domain.Enums;

namespace WMS.Application.Inventory.DTOs;

public record InventoryQuery(
    string? Search, string? Category, string? Zone,
    ItemStatus? Status, int Page = 1, int Limit = 20);

