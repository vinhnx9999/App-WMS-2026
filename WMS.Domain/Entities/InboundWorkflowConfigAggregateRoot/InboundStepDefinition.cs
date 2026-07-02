using WMS.Domain.Enums;

namespace WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;

public record InboundStepDefinition(InboundStepType StepType, int Sequence, string DisplayName);
