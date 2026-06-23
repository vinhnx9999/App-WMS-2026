using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Master;

namespace WMS.Infrastructure.Configurations;

public class SupplierConfiguration : BaseEntityConfiguration<Supplier>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(255);
        builder.Property(b => b.Email).HasMaxLength(255).IsRequired(false);
        builder.Property(b => b.Phone).HasMaxLength(20).IsRequired(false);
        builder.Property(b => b.Contact).HasMaxLength(255).IsRequired(false);
        builder.Property(b => b.Address).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();

        builder.ToTable("suppliers");
    }
}
