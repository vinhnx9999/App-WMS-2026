using WMS.Domain.Common;

namespace WMS.Domain.Entities.Warehouses;

public class Block : BaseEntity
{
    private Block() { }

    public Block(Guid tenantId, Guid warehouseId, Guid areaId, string name, string code, bool isDefault = false)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        AreaId = areaId;
        Name = name;
        Code = code;
        IsDefault = isDefault;
    }

    public Guid WarehouseId { get; private set; }
    public Guid AreaId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsDefault { get; private set; }
}
