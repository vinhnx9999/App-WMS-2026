using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;

public class InboundWorkflowStep : BaseEntity
{
    private InboundWorkflowStep() { }

    public InboundWorkflowStep(InboundStepType stepType, int sequence, string displayName)
    {
        StepType = stepType;
        Sequence = sequence;
        DisplayName = displayName;
    }

    public Guid WorkflowConfigId { get; private set; }
    public InboundStepType StepType { get; private set; }
    public int Sequence { get; private set; }
    public string DisplayName { get; private set; } = null!;
}
