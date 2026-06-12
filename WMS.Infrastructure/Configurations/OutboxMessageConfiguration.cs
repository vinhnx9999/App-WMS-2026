using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.ErpSync;

namespace WMS.Infrastructure.Configurations;

public class OutboxMessageConfiguration : BaseEntityConfiguration<OutboxMessage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
    }
}