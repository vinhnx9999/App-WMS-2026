using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Services;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using ITransaction = WMS.Domain.Interfaces.ITransaction;

namespace DP.AppWMS.Tests;

public class InboundServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ITransaction> _txMock;
    private readonly Mock<ICurrentUser> _userMock;

    public InboundServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _txMock = new Mock<ITransaction>();
        _userMock = new Mock<ICurrentUser>();

        // Setup: BeginTransaction returns mock transaction
        _uowMock
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldCommitTransaction()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var orderRepo = new Mock<IRepository<InboundOrder>>();
        var order = WithId(new InboundOrder
        {
            OrderNumber = "PO-TEST-001",
            Status = InboundStatus.Pending,
            Items =
            [
                new()
                {
                    InventoryItemId = itemId,
                    Quantity = 100,
                    ReceivedQuantity = 0,
                }
            ]
        }, orderId);

        orderRepo
            .Setup(x => x.Query())
            .Returns(new List<InboundOrder> { order }
                .AsQueryable());

        var invRepo = new Mock<IRepository<InventoryItem>>();
        invRepo
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WithId(new InventoryItem
            {
                SkuCode = "TEST-001",
                Quantity = 50,
                MinQuantity = 10,
            }, itemId));

        var auditRepo = new Mock<IRepository<AuditLog>>();

        _uowMock.Setup(x => x.Repository<InboundOrder>())
            .Returns(orderRepo.Object);
        _uowMock.Setup(x => x.Repository<InventoryItem>())
            .Returns(invRepo.Object);
        _uowMock.Setup(x => x.Repository<AuditLog>())
            .Returns(auditRepo.Object);
        _uowMock.Setup(x => x.Repository<Zone>())
            .Returns(new Mock<IRepository<Zone>>().Object);

        // Act
        var svc = new InboundService(_uowMock.Object, _userMock.Object, null);
        await svc.ReceiveAsync(orderId, new(
            [
                new(itemId, 95, "5 units damaged")
            ]), CancellationToken.None);

        // Assert — verify transaction lifecycle
        _uowMock.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _txMock.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify inventory updated
        var inv = await invRepo.Object.GetByIdAsync(itemId);
        inv!.Quantity.Should().Be(145); // 50 + 95

        // Verify order completed
        order.Status.Should().Be(InboundStatus.Completed);
    }

    [Fact]
    public async Task ReceiveAsync_WhenException_ShouldNotCommit()
    {
        // Arrange — setup to throw on SaveChanges
        _uowMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB error"));

        // ... setup repos ...
        var svc = new InboundService(_uowMock.Object, _userMock.Object, null);
        var orderId = Guid.NewGuid();
        var request = new ReceiveInboundRequest(
            [
                new(Guid.NewGuid(), 10, "Test")
            ]);
        // Act & Assert
        var act = () => svc.ReceiveAsync(orderId, request, CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();

        // Commit should NOT have been called
        _txMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TEntity WithId<TEntity>(TEntity entity, Guid id)
        where TEntity : BaseEntity
    {
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(entity, id);

        return entity;
    }
}