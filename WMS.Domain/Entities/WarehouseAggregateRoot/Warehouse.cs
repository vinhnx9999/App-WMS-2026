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

    private List<WarehouseArea> _areas = [];

    /// <summary>
    /// Read-only access to Areas. All mutations must go through aggregate root methods.
    /// </summary>
    public IReadOnlyCollection<WarehouseArea> Areas => _areas.AsReadOnly();

    public WarehouseArea AddArea(string name, string code, bool isDefault = false, bool isAutomated = false)
    {
        var area = WarehouseArea.Create(TenantId, Id, name, code, isDefault, isAutomated);
        _areas.Add(area);
        return area;
    }

    public Block AddBlock(Guid areaId, string name, string code, bool isDefault = false)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId && !a.IsDeleted);
        if (area == null)
        {
            throw new InvalidOperationException($"Area with ID {areaId} not found in warehouse.");
        }
        var block = Block.Create(TenantId, Id, areaId, name, code, isDefault);
        area.AddBlock(block);
        return block;
    }

    /// <summary>
    /// Soft-delete an area and all its blocks.
    /// </summary>
    public void RemoveArea(Guid areaId, string? deletedBy = null)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId && !a.IsDeleted);
        if (area == null)
        {
            throw new InvalidOperationException($"Area with ID {areaId} not found in warehouse.");
        }
        area.MarkDeleted(deletedBy);
    }

    /// <summary>
    /// Soft-delete a block within a specific area.
    /// </summary>
    public void RemoveBlock(Guid areaId, Guid blockId, string? deletedBy = null)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId && !a.IsDeleted);
        if (area == null)
        {
            throw new InvalidOperationException($"Area with ID {areaId} not found in warehouse.");
        }
        area.RemoveBlock(blockId, deletedBy);
    }

    /// <summary>
    /// Default structure to create if not exists 
    /// </summary>
    /// <returns></returns>
    public (WarehouseArea DefaultArea, Block DefaultBlock) EnsureDefaultStructure()
    {
        var defaultArea = _areas.FirstOrDefault(a => a.IsDefault && !a.IsDeleted);
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
