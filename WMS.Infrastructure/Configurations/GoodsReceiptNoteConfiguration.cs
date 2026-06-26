using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class GoodsReceiptNoteConfiguration : BaseEntityConfiguration<GoodsReceiptNote>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.HasIndex(x => x.GrnNumber).IsUnique();
        builder.ToTable("goods_receipt_notes");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("GoodsReceiptNoteId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(GoodsReceiptNote.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class GoodsReceiptNoteItemConfiguration : BaseEntityConfiguration<GoodsReceiptNoteItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GoodsReceiptNoteItem> builder)
    {
        builder.ToTable("goods_receipt_note_items");
        builder.HasIndex(x => x.SkuId);
    }
}
