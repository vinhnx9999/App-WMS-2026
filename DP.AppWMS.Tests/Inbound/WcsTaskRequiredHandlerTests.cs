using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.Handlers;
using WMS.Domain.Entities.WcsIntegration;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public class WcsTaskRequiredHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenSingleItem_ShouldCreateOneWcsTaskAndOneWcsSubTaskAsync()
    {
        // Arrange
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new WmsDbContext(options, Mock.Of<ICurrentUser>(), Mock.Of<MediatR.IMediator>());
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var uow = new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);

        var seqGenMock = new Mock<ISequenceCodeGenerator>();
        seqGenMock.Setup(s => s.NextAsync(_tenantId, CodeSequenceTypes.WcsInboundTask, It.IsAny<CancellationToken>()))
            .ReturnsAsync("WCS-IN-001");

        var warehouseId = Guid.NewGuid();
        var putawayTaskId = Guid.NewGuid();
        var palletCode = "PL-WCS-001";
        var locationId = Guid.NewGuid();

        var items = new List<WcsMovementItem>
        {
            new WcsMovementItem(palletCode, "3.1.2", "WCS-B01", locationId)
        };

        var taskEvent = new WcsTaskRequiredEvent(_tenantId, warehouseId, putawayTaskId, items);
        var handler = new WcsTaskRequiredHandler(uow, seqGenMock.Object);

        // Act
        await handler.Handle(taskEvent, CancellationToken.None);

        // Assert
        var wcsTasks = await db.Set<WcsTask>().Include(t => t.SubTasks).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        wcsTasks.Should().ContainSingle();

        var task = wcsTasks.Single();
        task.TenantId.Should().Be(_tenantId);
        task.WarehouseId.Should().Be(warehouseId);
        task.WmsPutawayTaskId.Should().Be(putawayTaskId);
        task.WcsBlockId.Should().Be("WCS-B01");
        task.WcsTaskNumber.Should().Be("WCS-IN-001");
        task.TaskType.Should().Be(WcsTaskTypes.Inbound);
        task.Status.Should().Be(WcsTaskStatus.Pending);

        task.SubTasks.Should().ContainSingle();
        var subTask = task.SubTasks.Single();
        subTask.TenantId.Should().Be(_tenantId);
        subTask.PalletCode.Should().Be(palletCode);
        subTask.ToLocationCode.Should().Be("3.1.2");
        subTask.FromLocationCode.Should().Be("0.0.0");
        subTask.Status.Should().Be(WcsTaskStatus.Pending);

        await connection.CloseAsync();
    }
}
