using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.PalletAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class PalletConfiguration : BaseEntityConfiguration<Pallet>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Pallet> builder)
    {
        builder.ToTable("pallets");

        builder.Property(e => e.PalletCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.PalletCode })
            .IsUnique();

        builder.Property(e => e.Material)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.IsMixSku)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
