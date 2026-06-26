using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class PutawayTaskConfiguration : BaseEntityConfiguration<PutawayTask>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PutawayTask> builder)
    {
        builder.HasIndex(x => x.PutawayTaskNumber).IsUnique();
        builder.ToTable("putaway_tasks");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("PutawayTaskId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(PutawayTask.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class PutawayTaskItemConfiguration : BaseEntityConfiguration<PutawayTaskItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PutawayTaskItem> builder)
    {
        builder.ToTable("putaway_task_items");
        builder.HasIndex(x => x.SkuId);
    }
}
