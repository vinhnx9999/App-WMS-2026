using WMS.Domain.Common;

namespace WMS.Domain.Entities.WarehouseAggregateRoot;

public class Block : BaseEntity
{
    private Block() { }

    internal Block(Guid tenantId, Guid warehouseId, Guid areaId, string name, string code, bool isDefault = false)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        AreaId = areaId;
        Name = name;
        Code = code;
        IsDefault = isDefault;
    }

    internal static Block Create(Guid tenantId, Guid warehouseId, Guid areaId, string name, string code, bool isDefault = false)
    {
        return new Block(tenantId, warehouseId, areaId, name, code, isDefault);
    }

    public Guid WarehouseId { get; private set; }
    public Guid AreaId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    public string? WcsBlockId { get; private set; }

    /// <summary>
    /// Set wcs block id 
    /// Only Ar can call this method 
    /// </summary>
    /// <param name="wcsBlockId"></param>
    internal void SetWcsBlockId(string wcsBlockId)
    {
        WcsBlockId = wcsBlockId;
    }
}
