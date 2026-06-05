using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuConfiguration : BaseEntityConfiguration<Sku>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");

        builder.HasIndex(x => new { x.TenantId, x.SkuCode })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.ProductId });

        builder.HasMany(x => x.AllowedUnits)
      .WithOne()
      .HasForeignKey(x => x.SkuId)
      .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(x => x.AllowedUnits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Attributes)
       .WithOne()
       .HasForeignKey(x => x.SkuId)
       .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(x => x.Attributes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
