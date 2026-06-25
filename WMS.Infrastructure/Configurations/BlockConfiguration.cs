using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WarehouseAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class BlockConfiguration : BaseEntityConfiguration<Block>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.Property(b => b.WarehouseId).IsRequired();
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.IsDefault).IsRequired().HasDefaultValue(false);

        // Unique index for IsDefault = true per Area
        builder.HasIndex(b => new { b.AreaId, b.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");
    }
}
