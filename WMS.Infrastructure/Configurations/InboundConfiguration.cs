using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.InboundOrderAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InboundConfiguration : BaseEntityConfiguration<InboundOrder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundOrder> builder)
    {
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.ToTable("inbound_orders");

        builder.HasMany(x => x.Items)
            .WithOne(x => x.InboundOrder)
            .HasForeignKey(x => x.InboundOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(InboundOrder.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
