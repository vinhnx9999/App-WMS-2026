using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Outbound;

namespace WMS.Infrastructure.Configurations;

public class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(b => b.Name).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(255).IsRequired(false);
        builder.Property(b => b.Phone).HasMaxLength(20).IsRequired(false);
        builder.Property(b => b.Type).HasMaxLength(50).IsRequired(false);

        builder.Ignore(b => b.Orders);

        builder.ToTable("customers");
    }
}
