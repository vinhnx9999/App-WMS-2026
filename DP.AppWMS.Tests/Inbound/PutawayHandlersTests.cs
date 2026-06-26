using FluentAssertions;
using Moq;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Domain.Events;
using WMS.Application.Inbound.Handlers;
using System.Linq.Expressions;

using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;

namespace DP.AppWMS.Tests.Inbound;

public class PutawayHandlersTests
{
    private readonly Mock<IRepository<InventoryItem>> _inventoryRepoMock;
    private readonly Mock<IRepository<InboundOrder>> _inboundOrderRepoMock;
    private readonly Mock<IRepository<GoodsReceiptNote>> _grnRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PutawayHandlersTests()
    {
        _inventoryRepoMock = new Mock<IRepository<InventoryItem>>();
        _inboundOrderRepoMock = new Mock<IRepository<InboundOrder>>();
        _grnRepoMock = new Mock<IRepository<GoodsReceiptNote>>();
        _uowMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(_tenantId);
        _currentUserMock.Setup(x => x.Email).Returns("test@wms.com");
    }

    [Fact]
    public async Task UpdateInventoryHandler_WhenNoMatchingInventoryExists_ShouldCreateNewInventoryItem()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();

        var inboundOrder = new InboundOrder
        {
            SupplierId = supplierId
        };
        _inboundOrderRepoMock
            .Setup(x => x.GetByIdAsync(inboundOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboundOrder);

        var task = new PutawayTask("PT-001", inboundOrderId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var taskItem = new PutawayTaskItem(skuId, 10, locationId);
        taskItem.CompletePutaway(locationId);
        task.AddItem(taskItem);

        // FindAsync returns empty list (meaning no match)
        _inventoryRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<InventoryItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        InventoryItem? createdItem = null;
        _inventoryRepoMock
            .Setup(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, ct) => createdItem = item)
            .ReturnsAsync((InventoryItem item, CancellationToken ct) => item);

        var handler = new UpdateInventoryHandler(_inventoryRepoMock.Object, _inboundOrderRepoMock.Object, _currentUserMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        createdItem.Should().NotBeNull();
        createdItem!.SkuId.Should().Be(skuId);
        createdItem.LocationId.Should().Be(locationId);
        createdItem.SupplierId.Should().Be(supplierId);
        createdItem.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task UpdateInventoryHandler_WhenMatchingInventoryExists_ShouldAddStockToExistingItem()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var inboundOrderId = Guid.NewGuid();

        var inboundOrder = new InboundOrder
        {
            SupplierId = supplierId
        };
        _inboundOrderRepoMock
            .Setup(x => x.GetByIdAsync(inboundOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboundOrder);

        var task = new PutawayTask("PT-001", inboundOrderId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var taskItem = new PutawayTaskItem(skuId, 10, locationId);
        taskItem.CompletePutaway(locationId);
        task.AddItem(taskItem);

        var existingItem = InventoryItem.Create(_tenantId, skuId, locationId, supplierId, null, null, 25, 0m, DateTime.UtcNow, null);

        // FindAsync returns the existing item
        _inventoryRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<InventoryItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { existingItem });

        var handler = new UpdateInventoryHandler(_inventoryRepoMock.Object, _inboundOrderRepoMock.Object, _currentUserMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        existingItem.Quantity.Should().Be(35); // 25 + 10
        _inventoryRepoMock.Verify(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepoMock.Verify(x => x.UpdateAsync(existingItem), Times.Once);
    }

    [Fact]
    public async Task GenerateGrnHandler_ShouldCreateGoodsReceiptNote()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var task = new PutawayTask("PT-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var taskItem = new PutawayTaskItem(skuId, 10, locationId);
        taskItem.CompletePutaway(locationId);
        task.AddItem(taskItem);

        GoodsReceiptNote? generatedGrn = null;
        _grnRepoMock
            .Setup(x => x.AddAsync(It.IsAny<GoodsReceiptNote>(), It.IsAny<CancellationToken>()))
            .Callback<GoodsReceiptNote, CancellationToken>((grn, ct) => generatedGrn = grn)
            .ReturnsAsync((GoodsReceiptNote grn, CancellationToken ct) => grn);

        var handler = new GenerateGrnHandler(_grnRepoMock.Object, _currentUserMock.Object);
        var notification = new PutawayTaskCompletedEvent(task);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        generatedGrn.Should().NotBeNull();
        generatedGrn!.PutawayTaskId.Should().Be(task.Id);
        generatedGrn.Items.Should().HaveCount(1);
        generatedGrn.Items.First().SkuId.Should().Be(skuId);
        generatedGrn.Items.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task InboundReceiptCompletedEventHandler_ShouldAddHistory()
    {
        var historyRepoMock = new Mock<IRepository<InboundOrderHistory>>();
        InboundOrderHistory? loggedHistory = null;
        historyRepoMock
            .Setup(x => x.AddAsync(It.IsAny<InboundOrderHistory>(), It.IsAny<CancellationToken>()))
            .Callback<InboundOrderHistory, CancellationToken>((h, ct) => loggedHistory = h)
            .ReturnsAsync((InboundOrderHistory h, CancellationToken ct) => h);

        var receipt = new InboundReceipt("REC-001", Guid.NewGuid(), Guid.NewGuid());
        var handler = new InboundReceiptCompletedEventHandler(historyRepoMock.Object, _currentUserMock.Object);
        var notification = new InboundReceiptCompletedEvent(receipt);

        await handler.Handle(notification, CancellationToken.None);

        loggedHistory.Should().NotBeNull();
        loggedHistory!.InboundOrderId.Should().Be(receipt.InboundOrderId!.Value);
        loggedHistory.Step.Should().Be("Receive");
    }
}
