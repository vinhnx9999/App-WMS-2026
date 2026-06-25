using WMS.Domain.Common;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.Warehouses;

public class LocationEntity : BaseEntity, IAggregateRoot
{
    private LocationEntity() { }

    public LocationEntity(
        Guid tenantId,
        Guid warehouseId,
        Guid areaId,
        Guid blockId,
        Guid? zoneId,
        string name,
        int? x = null,
        int? y = null,
        int? z = null)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        AreaId = areaId;
        BlockId = blockId;
        ZoneId = zoneId;
        Name = name;
        X = x;
        Y = y;
        Z = z;
    }

    public Guid WarehouseId { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid BlockId { get; private set; }
    public Guid? ZoneId { get; private set; }
    public string Name { get; private set; } = null!;


    // Coordinates
    /// <summary>
    /// Bay
    /// </summary>
    public int? X { get; private set; }

    /// <summary>
    /// Row
    /// </summary>
    public int? Y { get; private set; }
    /// <summary>
    /// Floor
    /// </summary>
    public int? Z { get; private set; }

}
