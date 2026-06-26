using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.QcInspectionAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class QcInspectionConfiguration : BaseEntityConfiguration<QcInspection>
{
    protected override void ConfigureEntity(EntityTypeBuilder<QcInspection> builder)
    {
        builder.HasIndex(x => x.InspectionNumber).IsUnique();
        builder.ToTable("qc_inspections");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("QcInspectionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(QcInspection.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class QcInspectionItemConfiguration : BaseEntityConfiguration<QcInspectionItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<QcInspectionItem> builder)
    {
        builder.ToTable("qc_inspection_items");
        builder.HasIndex(x => x.SkuId);
    }
}
