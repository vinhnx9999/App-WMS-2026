using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WcsIntegration;

namespace WMS.Infrastructure.Configurations;

public class WcsSubTaskHistoryConfiguration : BaseEntityConfiguration<WcsSubTaskHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WcsSubTaskHistory> builder)
    {
        builder.ToTable("wcs_sub_task_histories");
        builder.Property(x => x.Robot).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.WcsSubTaskId);
    }
}
