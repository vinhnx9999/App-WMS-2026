using WMS.Domain.Common;

namespace WMS.Domain.Entities.Warehouses;

public class LocationEntity : BaseEntity
{
    public Guid? ZoneId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Zone? Zone { get; set; }
    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];

    public string? ZoneCode { get; set; }
}
