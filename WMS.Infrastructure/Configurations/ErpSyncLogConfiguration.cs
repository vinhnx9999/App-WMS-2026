using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.ErpSync;

namespace WMS.Infrastructure.Configurations;

public class ErpSyncLogConfiguration : BaseEntityConfiguration<ErpSyncLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ErpSyncLog> builder)
    {
        builder.ToTable("erp_sync_logs");
    }
}
