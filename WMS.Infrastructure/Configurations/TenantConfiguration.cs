using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Security;

namespace WMS.Infrastructure.Configurations;

public class TenantConfiguration : BaseEntityConfiguration<Tenant>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(b => b.DisplayName).IsRequired();

        builder.ToTable("tenants");
    }
}
