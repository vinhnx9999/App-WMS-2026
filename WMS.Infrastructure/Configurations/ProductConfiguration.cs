using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Product;

namespace WMS.Infrastructure.Configurations;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasIndex(x => new { x.TenantId, x.ProductCode })
            .IsUnique();

        builder.HasMany(x => x.Skus)
                 .WithOne()
                 .HasForeignKey(x => x.ProductId)
                 .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(x => x.Skus)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
