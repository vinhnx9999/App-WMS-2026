using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Domain.Entities.InventoryAggregateRoot;

namespace WMS.Domain.Entities.Warehouses;

public class Zone : BaseEntity, IAggregateRoot
{
    private Zone() { }

    public Zone(Guid tenantId, string name, string zoneCode, ZoneType zoneType, string? description = null)
    {
        TenantId = tenantId;
        Name = name;
        ZoneCode = zoneCode;
        ZoneType = zoneType;
        Description = description;
    }

    public string Name { get; set; } = null!;
    public string ZoneCode { get; set; } = null!;
    public ZoneType ZoneType { get; set; } = ZoneType.Standard;
    public string? Description { get; set; }
    public ICollection<InventoryItem> Items { get; set; } = [];
}
