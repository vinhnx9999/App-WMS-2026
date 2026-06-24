using WMS.Domain.Enums;

namespace WMS.Domain.Interfaces.Warehouses;

public interface IWarehouseRuleResolutionService
{
    /// <summary>
    /// Resolves the most specific WarehouseRuleSetting matching the given context.
    /// Returns the RuleType, or FIFO if no rule matches.
    /// </summary>
    Task<WarehouseRuleType> ResolvePickingStrategyAsync(
        Guid warehouseId,
        Guid locationId,
        Guid? skuId = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);
}
