using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.RuleAggregateRoot;

public class WarehouseRuleSetting : BaseEntity, IAggregateRoot
{
    private WarehouseRuleSetting() { }

    public Guid WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? ZoneId { get; private set; }
    public Guid? BlockId { get; private set; }
    public Guid? AreaId { get; private set; }
    public Guid? SkuId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public WarehouseRuleType RuleType { get; private set; } = WarehouseRuleType.FIFO;

    public static WarehouseRuleSetting Create(
        Guid tenantId,
        Guid warehouseId,
        Guid? locationId,
        Guid? zoneId,
        Guid? blockId,
        Guid? areaId,
        Guid? skuId,
        Guid? supplierId,
        WarehouseRuleType ruleType,
        bool isBlockDefault = false,
        bool isAreaDefault = false)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new DomainException("INVALID_WAREHOUSE_ID", "WarehouseId is required.");
        }

        int spatialCount = 0;
        if (locationId.HasValue && locationId != Guid.Empty) spatialCount++;
        if (zoneId.HasValue && zoneId != Guid.Empty) spatialCount++;
        if (blockId.HasValue && blockId != Guid.Empty) spatialCount++;
        if (areaId.HasValue && areaId != Guid.Empty) spatialCount++;

        if (spatialCount > 1)
        {
            throw new DomainException("INVALID_RULE_SCOPE", "At most one spatial constraint (Location, Zone, Block, or Area) can be set.");
        }

        if (blockId.HasValue && isBlockDefault)
        {
            throw new DomainException("INVALID_RULE_TARGET", "Cannot create a rule targeting the Default Block.");
        }

        if (areaId.HasValue && isAreaDefault)
        {
            throw new DomainException("INVALID_RULE_TARGET", "Cannot create a rule targeting the Default Area.");
        }

        return new WarehouseRuleSetting
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            ZoneId = zoneId,
            BlockId = blockId,
            AreaId = areaId,
            SkuId = skuId,
            SupplierId = supplierId,
            RuleType = ruleType
        };
    }
}