using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuSpecificationConfiguration : BaseEntityConfiguration<SkuSpecification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuSpecification> builder)
    {
        builder.ToTable("sku_specifications");

        builder.HasIndex(e => new { e.TenantId, e.SkuId, e.SpecificationId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.Sku)
            .WithMany(e => e.SkuSpecifications)
            .HasForeignKey(e => e.SkuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Specification)
            .WithMany(e => e.SkuSpecifications)
            .HasForeignKey(e => e.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
