using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Warehouses;

namespace WMS.Infrastructure.Configurations;

public class LocationEntityConfiguration : BaseEntityConfiguration<LocationEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LocationEntity> builder)
    {
        builder.ToTable("locations");

        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);

        builder.Property(l => l.WarehouseId).IsRequired();
        builder.Property(l => l.AreaId).IsRequired();
        builder.Property(l => l.BlockId).IsRequired();

        // Coordinates
        builder.Property(l => l.CoorX);
        builder.Property(l => l.CoorY);
        builder.Property(l => l.CoorZ);

        // Zone relationship with SetNull on delete
        builder.HasOne<Zone>()
            .WithMany()
            .HasForeignKey(l => l.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.WarehouseId);
        builder.HasIndex(l => l.BlockId);
        builder.HasIndex(l => l.ZoneId);
    }
}
