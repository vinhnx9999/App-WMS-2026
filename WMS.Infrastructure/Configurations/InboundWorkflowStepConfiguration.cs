using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class InboundWorkflowStepConfiguration : BaseEntityConfiguration<InboundWorkflowStep>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboundWorkflowStep> builder)
    {
        builder.ToTable("inbound_workflow_steps");

        builder.Property(s => s.WorkflowConfigId).IsRequired();
        builder.Property(s => s.StepType).IsRequired();
        builder.Property(s => s.Sequence).IsRequired();
        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(200);
    }
}
