using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuConfiguration : BaseEntityConfiguration<Sku>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");

        builder.HasMany(x => x.Attributes)
            .WithOne()
            .HasForeignKey(x => x.SkuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedUnits)
            .WithOne()
            .HasForeignKey(x => x.SkuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Attributes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.AllowedUnits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
