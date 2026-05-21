using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuConfiguration : BaseEntityConfiguration<SkuEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuEntity> builder)
    {
        builder.ToTable("skus");

        builder.Property(e => e.SkuCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.HasIndex(e => new { e.TenantId, e.CategoryId });

        // can't have 2 skucode with same tenant
        builder.HasIndex(e => new { e.TenantId, e.SkuCode })
            .IsUnique();

        builder.Ignore(e => e.Category);
        builder.Ignore(e => e.InventoryItems);
    }
}
