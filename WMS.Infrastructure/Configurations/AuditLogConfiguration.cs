using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(b => b.Action).IsRequired();
        builder.Property(b => b.TableName).IsRequired();
        builder.ToTable("audit_logs");
    }
}
