using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InboundWorkflowConfigConfiguration : BaseEntityConfiguration<InboundWorkflowConfig>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundWorkflowConfig> builder)
    {
        builder.ToTable("inbound_workflow_configs");

        builder.Property(c => c.WarehouseId);
        builder.Property(c => c.SupplierId);
        builder.Property(c => c.ProductCategoryId);
        builder.Property(c => c.AllowOverReceive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.OverReceiveTolerancePercentage);

        builder.HasMany(c => c.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
