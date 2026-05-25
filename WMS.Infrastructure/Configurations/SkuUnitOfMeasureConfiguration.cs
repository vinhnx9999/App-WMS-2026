using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuUnitOfMeasureConfiguration : BaseEntityConfiguration<SkuUnitOfMeasure>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuUnitOfMeasure> builder)
    {
        builder.ToTable("sku_unit_of_measures");

        builder.Property(e => e.ConversionFactor)
            .HasPrecision(18, 6);

        builder.HasIndex(e => new { e.TenantId, e.SkuId, e.UnitOfMeasureId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.Sku)
            .WithMany(e => e.SkuUnitOfMeasures)
            .HasForeignKey(e => e.SkuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany(e => e.SkuUnitOfMeasures)
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
