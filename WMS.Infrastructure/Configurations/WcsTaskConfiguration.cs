using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.WcsIntegration;

namespace WMS.Infrastructure.Configurations;

public class WcsTaskConfiguration : BaseEntityConfiguration<WcsTask>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WcsTask> builder)
    {
        builder.ToTable("wcs_tasks");
        builder.Property(x => x.WcsTaskNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.WcsBlockId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TaskType).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.WcsTaskNumber).IsUnique();
        builder.HasIndex(x => x.WmsPutawayTaskId);

        builder.HasMany(x => x.SubTasks)
            .WithOne()
            .HasForeignKey(x => x.WcsTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(WcsTask.SubTasks))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
