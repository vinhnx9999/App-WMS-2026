using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Product;

namespace WMS.Infrastructure.Configurations;

public class SkuImportSessionConfiguration : BaseEntityConfiguration<SkuImportSession>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuImportSession> builder)
    {
        builder.ToTable("sku_import_sessions");

        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceFileName).HasMaxLength(255);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);

        builder.HasMany(x => x.Rows)
            .WithOne()
            .HasForeignKey(x => x.ImportSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Rows)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
