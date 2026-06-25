using WMS.Domain.Common;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.WarehouseAggregateRoot;

public class Warehouse : BaseEntity, IAggregateRoot
{
    private Warehouse() { }

    public Warehouse(Guid tenantId, string name, string code, string? address = null)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        Address = address;
    }

    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Address { get; private set; }

    public ICollection<WarehouseArea> Areas { get; private set; } = [];

    /// <summary>
    /// Default structure to create if not exists 
    /// </summary>
    /// <returns></returns>
    public (WarehouseArea DefaultArea, Block DefaultBlock) EnsureDefaultStructure()
    {
        var defaultArea = Areas.FirstOrDefault(a => a.IsDefault && !a.IsDeleted);
        if (defaultArea == null)
        {
            defaultArea = new WarehouseArea(TenantId, Id, "Default", "DEFAULT", true);
            Areas.Add(defaultArea);
        }

        var defaultBlock = defaultArea.Blocks.FirstOrDefault(b => b.IsDefault && !b.IsDeleted);
        if (defaultBlock == null)
        {
            defaultBlock = new Block(TenantId, Id, defaultArea.Id, "Default", "DEFAULT", true);
            defaultArea.Blocks.Add(defaultBlock);
        }

        return (defaultArea, defaultBlock);
    }
}
