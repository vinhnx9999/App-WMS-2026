using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Services;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;
using ITransaction = WMS.Domain.Interfaces.ITransaction;

namespace DP.AppWMS.Tests;

public class InboundServiceTests
{
    private readonly Mock<ITransaction> _txMock;
    private readonly Mock<ICurrentUser> _userMock;

    public InboundServiceTests()
    {
        _txMock = new Mock<ITransaction>();
        _userMock = new Mock<ICurrentUser>();
    }

    private async Task<(SqliteConnection Connection, WmsDbContext Db, UnitOfWork Uow)> SetupInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new WmsDbContext(options, _userMock.Object, Mock.Of<MediatR.IMediator>());
        await db.Database.EnsureCreatedAsync();
        var uow = new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);
        return (connection, db, uow);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldCommitTransaction()
    {
        // Arrange
        var (connection, db, realUow) = await SetupInMemoryDbAsync();

        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid(); // SkuId

        // Seed order
        var order = InboundOrder.Create(Guid.Empty, "PO-TEST-001", null, null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(order, orderId);
        order.AddItem(itemId, 100, null);
        db.InboundOrders.Add(order);

        // Seed location
        var location = LocationEntity.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-001");
        db.Locations.Add(location);

        // Seed inventory
        var inventory = InventoryItem.Create(Guid.Empty, itemId, location.Id, null, null, null, 50, 0, DateTime.UtcNow, null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(inventory, itemId);
        db.InventoryItems.Add(inventory);

        await db.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(x => x.Repository<InboundOrder>()).Returns(realUow.Repository<InboundOrder>());
        uowMock.Setup(x => x.Repository<InventoryItem>()).Returns(realUow.Repository<InventoryItem>());
        uowMock.Setup(x => x.Repository<LocationEntity>()).Returns(realUow.Repository<LocationEntity>());
        uowMock.Setup(x => x.Repository<AuditLog>()).Returns(realUow.Repository<AuditLog>());
        uowMock.Setup(x => x.Repository<Zone>()).Returns(realUow.Repository<Zone>());

        uowMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => realUow.SaveChangesAsync());

        // Act
        var svc = new InboundService(uowMock.Object, _userMock.Object, null);
        await svc.ReceiveAsync(orderId, new(
            [
                new(itemId, 95, "5 units damaged")
            ]), CancellationToken.None);

        // Assert — verify transaction lifecycle
        uowMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        uowMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _txMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify inventory updated
        var inv = await realUow.Repository<InventoryItem>().GetByIdAsync(itemId);
        inv!.Quantity.Should().Be(145); // 50 + 95

        // Verify order completed
        var updatedOrder = await realUow.Repository<InboundOrder>().GetByIdAsync(orderId);
        updatedOrder!.Status.Should().Be(InboundStatus.Completed);
    }

    [Fact]
    public async Task ReceiveAsync_WhenException_ShouldNotCommit()
    {
        // Arrange — setup to throw on SaveChanges
        var (connection, db, realUow) = await SetupInMemoryDbAsync();

        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Seed order
        var order = InboundOrder.Create(Guid.Empty, "PO-TEST-001", null, null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(order, orderId);
        order.AddItem(itemId, 100, null);
        db.InboundOrders.Add(order);

        // Seed location
        var location = LocationEntity.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "LOC-001");
        db.Locations.Add(location);

        // Seed inventory
        var inventory = InventoryItem.Create(Guid.Empty, itemId, location.Id, null, null, null, 50, 0, DateTime.UtcNow, null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(inventory, itemId);
        db.InventoryItems.Add(inventory);

        await db.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(x => x.Repository<InboundOrder>()).Returns(realUow.Repository<InboundOrder>());
        uowMock.Setup(x => x.Repository<InventoryItem>()).Returns(realUow.Repository<InventoryItem>());
        uowMock.Setup(x => x.Repository<LocationEntity>()).Returns(realUow.Repository<LocationEntity>());

        uowMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB error"));

        var svc = new InboundService(uowMock.Object, _userMock.Object, null);
        var request = new ReceiveInboundRequest(
            [
                new(itemId, 10, "Test")
            ]);

        // Act & Assert
        var act = () => svc.ReceiveAsync(orderId, request, CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();

        // Commit should NOT have been called
        _txMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}