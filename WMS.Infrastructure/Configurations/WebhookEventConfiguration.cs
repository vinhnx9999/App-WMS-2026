using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.ErpSync;

namespace WMS.Infrastructure.Configurations;

public class WebhookEventConfiguration : BaseEntityConfiguration<WebhookEvent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");
    }
}
