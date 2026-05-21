using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class InventoryConfiguration : BaseEntityConfiguration<InventoryItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasIndex(x => x.SkuId).IsUnique();

        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.Status);

        builder.ToTable("inventory_items");
    }
}