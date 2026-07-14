using FluentAssertions;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Enums;

namespace DP.AppWMS.Tests.Inbound;

public class InboundOrderTests
{
    [Fact]
    public void Create_ShouldInitializeWithApprovedStatus()
    {
        // Arrange & Act
        var order = InboundOrder.Create(Guid.NewGuid(), "PO-001", null, null);

        // Assert
        order.Status.Should().Be(InboundStatus.Approved);
    }
}
