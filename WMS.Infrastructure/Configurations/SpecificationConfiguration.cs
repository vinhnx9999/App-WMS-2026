using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SpecificationConfiguration : BaseEntityConfiguration<Specification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Specification> builder)
    {
        builder.ToTable("specifications");

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Name)
            .HasMaxLength(250);

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasMany(e => e.SkuSpecifications)
            .WithOne(e => e.Specification)
            .HasForeignKey(e => e.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
