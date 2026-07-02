using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WcsIntegration;

namespace WMS.Infrastructure.Configurations;

public class WcsSubTaskConfiguration : BaseEntityConfiguration<WcsSubTask>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WcsSubTask> builder)
    {
        builder.ToTable("wcs_sub_tasks");
        builder.Property(x => x.PalletCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FromLocationCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ToLocationCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.HasIndex(x => new { x.WcsTaskId, x.PalletCode }).IsUnique();
    }
}
