using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;

namespace DP.AppWMS.Tests.Inbound;

public class InboundWorkflowConfigTests
{
    [Fact]
    public void UpdateSteps_WhenStepsEmpty_ShouldThrowDomainException()
    {
        // Arrange
        var config = new InboundWorkflowConfig(Guid.NewGuid(), Guid.NewGuid(), null, null);
        var steps = Enumerable.Empty<InboundStepDefinition>();

        // Act
        var act = () => config.UpdateSteps(steps);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Workflow steps cannot be empty.");
    }

    [Fact]
    public void UpdateSteps_WhenPutawayIsMissing_ShouldThrowDomainException()
    {
        // Arrange
        var config = new InboundWorkflowConfig(Guid.NewGuid(), Guid.NewGuid(), null, null);
        var steps = new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 1, "PO"),
            new(InboundStepType.Receive, 2, "Receive"),
            new(InboundStepType.QC, 3, "QC")
        };

        // Act
        var act = () => config.UpdateSteps(steps);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Putaway step is mandatory in the workflow.");
    }

    [Fact]
    public void UpdateSteps_WhenPutawayIsNotLastStep_ShouldThrowDomainException()
    {
        // Arrange
        var config = new InboundWorkflowConfig(Guid.NewGuid(), Guid.NewGuid(), null, null);
        var steps = new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 1, "PO"),
            new(InboundStepType.Putaway, 2, "Putaway"),
            new(InboundStepType.QC, 3, "QC")
        };

        // Act
        var act = () => config.UpdateSteps(steps);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Putaway step must be the final step in the sequence.");
    }

    [Fact]
    public void UpdateSteps_WhenValidStepsProvided_ShouldUpdateSuccessfully()
    {
        // Arrange
        var config = new InboundWorkflowConfig(Guid.NewGuid(), Guid.NewGuid(), null, null);
        var steps = new List<InboundStepDefinition>
        {
            new(InboundStepType.Receive, 1, "Receive"),
            new(InboundStepType.QC, 2, "QC"),
            new(InboundStepType.Putaway, 3, "Putaway")
        };

        // Act
        config.UpdateSteps(steps);

        // Assert
        config.Steps.Should().HaveCount(3);
        config.Steps.Should().Contain(s => s.StepType == InboundStepType.Receive);
        config.Steps.Should().Contain(s => s.StepType == InboundStepType.QC);
        config.Steps.Should().Contain(s => s.StepType == InboundStepType.Putaway);
    }
}
