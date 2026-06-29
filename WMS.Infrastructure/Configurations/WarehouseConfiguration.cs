using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WarehouseAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class WarehouseConfiguration : BaseEntityConfiguration<Warehouse>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");

        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Code).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Address).HasMaxLength(500);

        builder.HasIndex(w => w.Code).IsUnique();

        // Areas relationship (stored in separate table)
        builder.HasMany(w => w.Areas)
            .WithOne()
            .HasForeignKey(a => a.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        // Use backing field _areas for materialization (collection is sealed as IReadOnlyCollection)
        builder.Navigation(w => w.Areas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
