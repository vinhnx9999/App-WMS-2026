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
        int? coorX = null,
        int? coorY = null,
        int? coorZ = null)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        AreaId = areaId;
        BlockId = blockId;
        ZoneId = zoneId;
        Name = name;
        CoorX = coorX;
        CoorY = coorY;
        CoorZ = coorZ;
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
    public int? CoorX { get; private set; }

    /// <summary>
    /// Row
    /// </summary>
    public int? CoorY { get; private set; }
    /// <summary>
    /// Floor
    /// </summary>
    public int? CoorZ { get; private set; }

}
