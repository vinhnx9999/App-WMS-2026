using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InboundOrderHistoryConfiguration : BaseEntityConfiguration<InboundOrderHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundOrderHistory> builder)
    {
        builder.ToTable("inbound_order_histories");
        builder.HasIndex(x => x.InboundOrderId);
        builder.HasIndex(x => x.UserId);
    }
}
