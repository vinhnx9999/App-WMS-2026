using WMS.Domain.Common;

namespace WMS.Domain.Entities.WarehouseAggregateRoot;

public class WarehouseArea : BaseEntity
{
    private WarehouseArea() { }

    public WarehouseArea(Guid tenantId, Guid warehouseId, string name, string code, bool isDefault = false)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        Name = name;
        Code = code;
        IsDefault = isDefault;
    }

    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public ICollection<Block> Blocks { get; private set; } = [];
}
