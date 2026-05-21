using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Inbound;

namespace WMS.Infrastructure.Configurations;

public class InboundItemConfiguration : BaseEntityConfiguration<InboundItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundItem> builder)
    {
        builder.HasIndex(b => b.InboundOrderId);
        builder.HasIndex(b => b.InventoryItemId);

        builder
            .HasOne(b => b.InboundOrder)
            .WithMany(b => b.Items)
            .HasForeignKey(b => b.InboundOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(b => b.InventoryItem);


        builder.ToTable("inbound_items");
    }
}
