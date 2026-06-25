using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InventoryConfiguration : BaseEntityConfiguration<InventoryItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.SkuId, x.LocationId, x.SupplierId, x.SerialNumber, x.PalletId, x.ExpiryDate })
            .HasFilter("\"IsDeleted\" = false")
            .AreNullsDistinct(false)
            .IsUnique();

        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.RowVersion)
            .IsConcurrencyToken();

        builder.ToTable("inventory_items");
    }
}