using WMS.Domain.Common;

namespace WMS.Domain.Entities.WarehouseAggregateRoot;

public class WarehouseArea : BaseEntity
{
    private WarehouseArea() { }

    internal WarehouseArea(Guid tenantId, Guid warehouseId, string name, string code, bool isDefault = false, bool isAutomated = false)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        Name = name;
        Code = code;
        IsDefault = isDefault;
        IsAutomated = isAutomated;
    }

    internal static WarehouseArea Create(Guid tenantId, Guid warehouseId, string name, string code, bool isDefault = false, bool isAutomated = false)
    {
        return new WarehouseArea(tenantId, warehouseId, name, code, isDefault, isAutomated);
    }

    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public bool IsAutomated { get; private set; }

    private readonly List<Block> _blocks = [];

    /// <summary>
    /// Read-only access to Blocks. All mutations must go through the Warehouse aggregate root.
    /// </summary>
    public IReadOnlyCollection<Block> Blocks => _blocks.AsReadOnly();

    /// <summary>
    /// Add a block to this area. Only callable within the domain assembly (via Warehouse aggregate root).
    /// </summary>
    internal void AddBlock(Block block)
    {
        _blocks.Add(block);
    }

    /// <summary>
    /// Soft-delete a block in this area. Only callable within the domain assembly (via Warehouse aggregate root).
    /// </summary>
    internal void RemoveBlock(Guid blockId, string? deletedBy = null)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId && !b.IsDeleted);
        if (block == null)
        {
            throw new DomainException($"Block with ID {blockId} not found in area.");
        }
        block.MarkDeleted(deletedBy);
    }
}
