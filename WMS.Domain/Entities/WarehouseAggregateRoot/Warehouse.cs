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

    public WarehouseArea AddArea(string name, string code, bool isDefault = false, bool isAutomated = false)
    {
        var area = WarehouseArea.Create(TenantId, Id, name, code, isDefault, isAutomated);
        Areas.Add(area);
        return area;
    }

    public Block AddBlock(Guid areaId, string name, string code, bool isDefault = false)
    {
        var area = Areas.FirstOrDefault(a => a.Id == areaId && !a.IsDeleted);
        if (area == null)
        {
            throw new InvalidOperationException($"Area with ID {areaId} not found in warehouse.");
        }
        var block = Block.Create(TenantId, Id, areaId, name, code, isDefault);
        area.Blocks.Add(block);
        return block;
    }

    /// <summary>
    /// Default structure to create if not exists 
    /// </summary>
    /// <returns></returns>
    public (WarehouseArea DefaultArea, Block DefaultBlock) EnsureDefaultStructure()
    {
        var defaultArea = Areas.FirstOrDefault(a => a.IsDefault && !a.IsDeleted);
        if (defaultArea == null)
        {
            defaultArea = AddArea("Default", "DEFAULT", true);
        }

        var defaultBlock = defaultArea.Blocks.FirstOrDefault(b => b.IsDefault && !b.IsDeleted);
        if (defaultBlock == null)
        {
            defaultBlock = AddBlock(defaultArea.Id, "Default", "DEFAULT", true);
        }

        return (defaultArea, defaultBlock);
    }
}
