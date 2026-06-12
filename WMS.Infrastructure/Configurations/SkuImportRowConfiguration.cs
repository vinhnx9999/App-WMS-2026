using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Product;

namespace WMS.Infrastructure.Configurations;

public class SkuImportRowConfiguration : BaseEntityConfiguration<SkuImportRow>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuImportRow> builder)
    {
        builder.ToTable("sku_import_rows");

        builder.Property(x => x.ProductCode).HasMaxLength(100);
        builder.Property(x => x.SkuCode).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.GoodsNature).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(x => x.ImportSessionId);
    }
}
