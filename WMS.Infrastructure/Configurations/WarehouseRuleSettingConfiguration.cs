using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.RuleAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class WarehouseRuleSettingConfiguration : BaseEntityConfiguration<WarehouseRuleSetting>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WarehouseRuleSetting> builder)
    {
        builder.ToTable("warehouse_rule_settings");

        builder.Property(r => r.WarehouseId).IsRequired();
        builder.Property(r => r.LocationId);
        builder.Property(r => r.ZoneId);
        builder.Property(r => r.BlockId);
        builder.Property(r => r.AreaId);
        builder.Property(r => r.SkuId);
        builder.Property(r => r.SupplierId);
        builder.Property(r => r.RuleType).IsRequired();

        // Unique index for the combination of Warehouse + Scope + Filter
        builder.HasIndex(r => new
        {
            r.WarehouseId,
            r.LocationId,
            r.ZoneId,
            r.BlockId,
            r.AreaId,
            r.SkuId,
            r.SupplierId
        })
        .IsUnique()
        .AreNullsDistinct(false);
    }
}
