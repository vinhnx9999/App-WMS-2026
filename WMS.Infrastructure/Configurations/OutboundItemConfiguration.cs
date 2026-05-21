using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Outbound;

namespace WMS.Infrastructure.Configurations
{
    public class OutboundItemConfiguration : BaseEntityConfiguration<OutboundItem>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<OutboundItem> builder)
        {
            builder.HasIndex(b => b.OutboundOrderId);
            builder.HasIndex(b => b.InventoryItemId);

            builder
          .HasOne(b => b.OutboundOrder)
          .WithMany(b => b.Items)
          .HasForeignKey(b => b.OutboundOrderId)
          .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(b => b.InventoryItem);



            builder.ToTable("outbound_items");
        }
    }
}