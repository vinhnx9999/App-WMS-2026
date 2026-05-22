using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Inbound;

namespace WMS.Infrastructure.Configurations;

public class InboundConfiguration : BaseEntityConfiguration<InboundOrder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundOrder> builder)
    {
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.ToTable("inbound_orders");
    }
}
