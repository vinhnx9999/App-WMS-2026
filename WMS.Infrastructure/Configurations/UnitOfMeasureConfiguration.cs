using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class UnitOfMeasureConfiguration : BaseEntityConfiguration<UnitOfMeasure>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures");

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Name)
            .HasMaxLength(250);

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasMany(e => e.SkuUnitOfMeasures)
            .WithOne(e => e.UnitOfMeasure)
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
