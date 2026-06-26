using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InboundReceiptConfiguration : BaseEntityConfiguration<InboundReceipt>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundReceipt> builder)
    {
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.ToTable("inbound_receipts");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("InboundReceiptId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(InboundReceipt.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class InboundReceiptItemConfiguration : BaseEntityConfiguration<InboundReceiptItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundReceiptItem> builder)
    {
        builder.ToTable("inbound_receipt_items");
        builder.HasIndex(x => x.SkuId);
    }
}
