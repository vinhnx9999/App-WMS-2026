using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WarehouseAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class WarehouseAreaConfiguration : BaseEntityConfiguration<WarehouseArea>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WarehouseArea> builder)
    {
        builder.ToTable("warehouse_areas");

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Code).IsRequired().HasMaxLength(50);
        builder.Property(a => a.IsDefault).IsRequired().HasDefaultValue(false);

        // Blocks relationship
        builder.HasMany(a => a.Blocks)
            .WithOne()
            .HasForeignKey(b => b.AreaId)
            .OnDelete(DeleteBehavior.NoAction);

        // Use backing field _blocks for materialization (collection is sealed as IReadOnlyCollection)
        builder.Navigation(a => a.Blocks).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique index for IsDefault = true per Warehouse
        builder.HasIndex(a => new { a.WarehouseId, a.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");
    }
}
