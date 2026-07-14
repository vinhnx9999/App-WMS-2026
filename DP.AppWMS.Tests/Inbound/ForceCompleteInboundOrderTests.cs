using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.Commands.ForceCompleteInboundOrder;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public class ForceCompleteInboundOrderTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;

    public ForceCompleteInboundOrderTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
    }

    private async Task<(SqliteConnection Connection, WmsDbContext Db, UnitOfWork Uow)> SetupInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new WmsDbContext(options, _currentUserMock.Object, Mock.Of<MediatR.IMediator>());
        await db.Database.EnsureCreatedAsync();
        var uow = new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);
        return (connection, db, uow);
    }

    [Fact]
    public async Task Handle_ShouldCompleteInboundOrderAndSetStatusToCompleted()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = InboundOrder.Create(tenantId, "PO-0001", null, "Notes");
        typeof(WMS.Domain.Common.BaseEntity).GetProperty(nameof(WMS.Domain.Common.BaseEntity.Id))!.SetValue(order, orderId);
        
        db.InboundOrders.Add(order);
        await db.SaveChangesAsync();

        var handler = new ForceCompleteInboundOrderCommandHandler(uow);
        var command = new ForceCompleteInboundOrderCommand(tenantId, orderId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedOrder = await db.InboundOrders.FirstOrDefaultAsync(x => x.Id == orderId);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(InboundStatus.Completed);
        updatedOrder.ReceivedDate.Should().NotBeNull();
    }
}
