using System.Reflection.PortableExecutable;
using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public class LocationEntity : BaseEntity, IAggregateRoot
{
    private LocationEntity() { }

    private LocationEntity(
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

    public static LocationEntity Create(
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
        // if ((coorZ, coorX, coorY) is not (null, null, null))
        // {
        //     name = $"{coorZ}.{coorX}.{coorY}";
        // }
        return new LocationEntity(
            tenantId,
            warehouseId,
            areaId,
            blockId,
            zoneId,
            name,
            coorX,
            coorY,
            coorZ);
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

    /// <summary>
    /// Location type
    /// </summary>
    public LocationType Type { get; private set; } = LocationType.STORAGE_SLOT;

    /// <summary>
    /// buffer Location
    /// </summary>
    public bool IsBuffer { get; private set; } = false;

    /// <summary>
    /// Location blocked status (e.g. for maintenance)
    /// </summary>
    public bool IsBlocked { get; private set; } = false;

    /// <summary>
    /// Set location type
    /// </summary>
    /// <param name="type"></param>
    public void SetType(LocationType type)
    {
        Type = type;
    }

    /// <summary>
    /// Set buffer
    /// </summary>
    /// <param name="isBuffer"></param>
    public void SetBuffer(bool isBuffer)
    {
        if (IsBlocked || Type != LocationType.STORAGE_SLOT)
        {
            throw new DomainException("Location is blocked");
        }
        IsBuffer = isBuffer;
    }

    /// <summary>
    /// Set location blocked status
    /// </summary>
    /// <param name="isBlocked"></param>
    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
    }

    public bool CanPutway()
    {
        return Type == LocationType.STORAGE_SLOT && !IsBlocked;
    }

    public string GetLocationCode()
    {
        return $"{CoorZ ?? 0}.{CoorX ?? 0}.{CoorY ?? 0}";
    }
}
