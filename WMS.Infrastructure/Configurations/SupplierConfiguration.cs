using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Inbound;

namespace WMS.Infrastructure.Configurations;

public class SupplierConfiguration : BaseEntityConfiguration<Supplier>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(b => b.Name).IsRequired();
        builder.Property(b => b.Email).HasMaxLength(255).IsRequired(false);
        builder.Property(b => b.Phone).HasMaxLength(20).IsRequired(false);
        builder.Property(b => b.Contact).HasMaxLength(255).IsRequired(false);

        builder.Ignore(b => b.Orders);

        builder.ToTable("suppliers");
    }
}
