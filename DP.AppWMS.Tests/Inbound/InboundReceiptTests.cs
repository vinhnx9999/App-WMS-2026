using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;

namespace DP.AppWMS.Tests.Inbound;

public class InboundReceiptTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void CompleteReceipt_WhenOverReceiveNotAllowed_AndExceedsExpected_ShouldThrowDomainException()
    {
        // Arrange
        var receipt = new InboundReceipt("REC-001", Guid.NewGuid(), Guid.NewGuid());
        var config = new InboundWorkflowConfig(_tenantId, Guid.NewGuid(), Guid.NewGuid(), null, allowOverReceive: false);
        
        // Act
        var act = () => receipt.CompleteReceipt(config, totalReceivedSoFarAcrossAllReceipts: 110, expectedPoQty: 100);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Over-receiving is not allowed for this inbound workflow.");
    }

    [Fact]
    public void CompleteReceipt_WhenOverReceiveAllowed_AndExceedsTolerance_ShouldThrowDomainException()
    {
        // Arrange
        var receipt = new InboundReceipt("REC-002", Guid.NewGuid(), Guid.NewGuid());
        var config = new InboundWorkflowConfig(_tenantId, Guid.NewGuid(), Guid.NewGuid(), null, allowOverReceive: true, overReceiveTolerancePercentage: 10m);
        
        // Act
        var act = () => receipt.CompleteReceipt(config, totalReceivedSoFarAcrossAllReceipts: 112, expectedPoQty: 100);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Received quantity exceeds the allowed tolerance limit of 10%.");
    }

    [Fact]
    public void CompleteReceipt_WhenOverReceiveAllowed_AndWithinTolerance_ShouldCompleteSuccessfully()
    {
        // Arrange
        var receipt = new InboundReceipt("REC-003", Guid.NewGuid(), Guid.NewGuid());
        var config = new InboundWorkflowConfig(_tenantId, Guid.NewGuid(), Guid.NewGuid(), null, allowOverReceive: true, overReceiveTolerancePercentage: 10m);
        
        // Act
        receipt.CompleteReceipt(config, totalReceivedSoFarAcrossAllReceipts: 109, expectedPoQty: 100);

        // Assert
        receipt.Status.Should().Be(ReceiptStatus.Completed);
    }

    [Fact]
    public void CompleteReceipt_WhenOverReceiveAllowed_WithNoToleranceLimit_ShouldCompleteSuccessfully()
    {
        // Arrange
        var receipt = new InboundReceipt("REC-004", Guid.NewGuid(), Guid.NewGuid());
        var config = new InboundWorkflowConfig(_tenantId, Guid.NewGuid(), Guid.NewGuid(), null, allowOverReceive: true, overReceiveTolerancePercentage: null);
        
        // Act
        receipt.CompleteReceipt(config, totalReceivedSoFarAcrossAllReceipts: 500, expectedPoQty: 100);

        // Assert
        receipt.Status.Should().Be(ReceiptStatus.Completed);
    }
}
