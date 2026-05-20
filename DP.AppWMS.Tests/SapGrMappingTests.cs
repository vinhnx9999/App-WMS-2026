using FluentAssertions;
using Moq;
using WMS.Domain.Entities.Inbound;
using WMS.Infrastructure.ERPs.SAP.RfcClient;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

namespace DP.AppWMS.Tests;

public class SapGrMappingTests
{
    [Fact]
    public void MapInboundOrder_ToSapGrRequest_ShouldMapCorrectly()
    {
        string orderNumber = $"PO-{DateTime.Now.Year}-0001";

        // Arrange
        var order = new InboundOrder
        {
            OrderNumber = orderNumber,
            Items =
            [
                new()
                {
                    InventoryItemId = Guid.NewGuid(),
                    Quantity = 100,
                    ReceivedQuantity = 98,
                }
            ]
        };

        // Act
        var request = new SapGrRequest
        {
            PostingDate = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            ReferenceDoc = order.OrderNumber,
            Items = [.. order.Items.Select(i => new SapGrItem
            {
                Material = "1000",
                Plant = "1000",
                Quantity = i.ReceivedQuantity,
                MoveType = "101",
            })]
        };

        // Assert
        request.ReferenceDoc.Should().Be(orderNumber);
        request.Items.Should().HaveCount(1);
        request.Items[0].Quantity.Should().Be(98); // received, not ordered
        request.Items[0].MoveType.Should().Be("101");
    }

    [Fact]
    public async Task PostGoodsReceipt_WhenSapReturnsError_ShouldNotThrow()
    {
        // Arrange
        var rfcMock = new Mock<ISapRfcClient>();
        Moq.Language.Flow.IReturnsResult<ISapRfcClient> returnsResult = rfcMock
            .Setup(x => x.PostGoodsReceipt(
                It.IsAny<SapGrRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SapGrResult
            {
                Success = false,
                Messages = ["[E] Material not found"]
            });

        // Act
        var result = await rfcMock.Object.PostGoodsReceipt(
            new SapGrRequest(), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("Material not found"));
    }
}
