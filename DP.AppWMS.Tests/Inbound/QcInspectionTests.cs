using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.QcInspectionAggregateRoot;

namespace DP.AppWMS.Tests.Inbound;

public class QcInspectionTests
{
    [Fact]
    public void StartInspection_WhenStatusIsPending_ShouldTransitionToInspecting()
    {
        // Arrange
        var inspection = new QcInspection("QC-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        inspection.StartInspection();

        // Assert
        inspection.Status.Should().Be(QcStatus.Inspecting);
    }

    [Fact]
    public void StartInspection_WhenStatusIsNotPending_ShouldThrowDomainException()
    {
        // Arrange
        var inspection = new QcInspection("QC-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inspection.StartInspection();

        // Act
        var act = () => inspection.StartInspection();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Inspection can only be started from Pending status.");
    }

    [Fact]
    public void CompleteInspection_WhenStatusIsInspecting_ShouldTransitionToCompleted()
    {
        // Arrange
        var inspection = new QcInspection("QC-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inspection.StartInspection();

        // Act
        inspection.CompleteInspection();

        // Assert
        inspection.Status.Should().Be(QcStatus.Completed);
    }

    [Fact]
    public void CompleteInspection_WhenStatusIsNotInspecting_ShouldThrowDomainException()
    {
        // Arrange
        var inspection = new QcInspection("QC-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = () => inspection.CompleteInspection();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Inspection can only be completed from Inspecting status.");
    }

    [Fact]
    public void CreateItem_WhenPassedAndFailedDoNotSumToReceived_ShouldThrowDomainException()
    {
        // Act
        var act = () => new QcInspectionItem(Guid.NewGuid(), receivedQuantity: 10, passedQuantity: 5, failedQuantity: 4);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Passed and failed quantities must sum up to the received quantity.");
    }
}
