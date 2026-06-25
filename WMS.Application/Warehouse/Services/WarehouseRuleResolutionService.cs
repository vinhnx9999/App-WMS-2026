using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities.RuleAggregateRoot;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Domain.Interfaces.Warehouses;

namespace WMS.Application.Warehouse.Services;

public class WarehouseRuleResolutionService(IUnitOfWork uow) : IWarehouseRuleResolutionService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<WarehouseRuleType> ResolvePickingStrategyAsync(
        Guid warehouseId,
        Guid locationId,
        Guid? skuId = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var location = await _uow.Repository<LocationEntity>().Query()
            .FirstOrDefaultAsync(l => l.Id == locationId && !l.IsDeleted, cancellationToken);

        if (location == null)
        {
            throw new KeyNotFoundException($"Location with ID '{locationId}' was not found.");
        }

        var rules = await _uow.Repository<WarehouseRuleSetting>().Query()
            .Where(r => r.WarehouseId == warehouseId && !r.IsDeleted)
            .Where(r =>
                (r.LocationId == null || r.LocationId == locationId) &&
                (r.ZoneId == null || (location.ZoneId != null && r.ZoneId == location.ZoneId)) &&
                (r.BlockId == null || r.BlockId == location.BlockId) &&
                (r.AreaId == null || r.AreaId == location.AreaId) &&
                (r.SkuId == null || (skuId != null && r.SkuId == skuId)) &&
                (r.SupplierId == null || (supplierId != null && r.SupplierId == supplierId))
            )
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return WarehouseRuleType.FIFO;
        }

        var scoredRules = rules.Select(r => new
        {
            Rule = r,
            Score = CalculateSpecificityScore(r, location, skuId, supplierId)
        });

        var winningRule = scoredRules
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Rule.CreatedAt)
            .First();

        return winningRule.Rule.RuleType;
    }

    private static int CalculateSpecificityScore(
        WarehouseRuleSetting rule,
        LocationEntity location,
        Guid? skuId,
        Guid? supplierId)
    {
        int score = 0;
        if (rule.LocationId.HasValue && rule.LocationId.Value == location.Id)
            score += 16;
        if (rule.ZoneId.HasValue && location.ZoneId.HasValue && rule.ZoneId.Value == location.ZoneId.Value)
            score += 8;
        if (rule.BlockId.HasValue && rule.BlockId.Value == location.BlockId)
            score += 4;
        if (rule.AreaId.HasValue && rule.AreaId.Value == location.AreaId)
            score += 2;
        if (rule.SkuId.HasValue && skuId.HasValue && rule.SkuId.Value == skuId.Value)
            score += 2;
        if (rule.SupplierId.HasValue && supplierId.HasValue && rule.SupplierId.Value == supplierId.Value)
            score += 1;

        return score;
    }
}
