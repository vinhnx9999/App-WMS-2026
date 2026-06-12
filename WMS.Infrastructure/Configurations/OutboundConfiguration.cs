using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Outbound;

namespace WMS.Infrastructure.Configurations;

public class OutboundConfiguration : BaseEntityConfiguration<OutboundOrder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OutboundOrder> builder)
    {

        // not mapping the navigation property to avoid circular references
        builder.Ignore(x => x.Customer);

        builder.HasIndex(x => x.ShipmentNumber).IsUnique();
        builder.ToTable("outbound_orders");
    }
}
