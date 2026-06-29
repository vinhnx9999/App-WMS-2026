using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Warehouse.Queries.GetLocationsWithOccupancy;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Warehouses;

public sealed class GetLocationsWithOccupancyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private static WmsDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        return new WmsDbContext(options, Mock.Of<ICurrentUser>(), Mock.Of<MediatR.IMediator>());
    }

    private static UnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoLocationsExist_ReturnsEmptyListAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new GetLocationsWithOccupancyQueryHandler(CreateUnitOfWork(db));
        var query = new GetLocationsWithOccupancyQuery(WarehouseId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyLocationsForRequestedWarehouseAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        var locA = WMS.Domain.Entities.LocationEntity.Create(TenantId, warehouseA, areaId, blockId, null, "Loc A", 1, 1, 1);
        var locB = WMS.Domain.Entities.LocationEntity.Create(TenantId, warehouseB, areaId, blockId, null, "Loc B", 2, 2, 1);

        db.Locations.AddRange(locA, locB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLocationsWithOccupancyQueryHandler(CreateUnitOfWork(db));
        var query = new GetLocationsWithOccupancyQuery(warehouseA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(locA.Id);
        result[0].Name.Should().Be("Loc A");
    }

    [Fact]
    public async Task Handle_MapsSpecialLocationTypesAndBlockedStatusesAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        // 1. Blocked location (should map to "blocked")
        var blockedLoc = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Blocked Loc", 1, 1, 1);
        blockedLoc.SetBlocked(true);

        // 2. Horizontal path (should map to "path")
        var pathLoc = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Path Loc", 2, 1, 1);
        pathLoc.SetType(WMS.Domain.Enums.LocationType.HORIZONTAL_PATH);

        // 3. Lift point (should map to "lift")
        var liftLoc = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Lift Loc", 3, 1, 1);
        liftLoc.SetType(WMS.Domain.Enums.LocationType.LIFT_POINT);

        db.Locations.AddRange(blockedLoc, pathLoc, liftLoc);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLocationsWithOccupancyQueryHandler(CreateUnitOfWork(db));
        var query = new GetLocationsWithOccupancyQuery(WarehouseId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);

        var mappedBlocked = result.Single(x => x.Id == blockedLoc.Id);
        mappedBlocked.OccupancyStatus.Should().Be("blocked");

        var mappedPath = result.Single(x => x.Id == pathLoc.Id);
        mappedPath.OccupancyStatus.Should().Be("path");

        var mappedLift = result.Single(x => x.Id == liftLoc.Id);
        mappedLift.OccupancyStatus.Should().Be("lift");
    }

    [Fact]
    public async Task Handle_MapsStorageSlotsBasedOnInventoryQuantityAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var areaId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        // 1. Empty storage slot (no inventory)
        var locEmpty = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Empty Loc", 1, 1, 1);

        // 2. Occupied storage slot (active inventory quantity > 0)
        var locOccupied = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Occupied Loc", 2, 1, 1);
        var invOccupied = WMS.Domain.Entities.InventoryAggregateRoot.InventoryItem.Create(
            TenantId, skuId, locOccupied.Id, null, null, null, 10, 100, DateTime.UtcNow, null);

        // 3. Storage slot with zero quantity inventory (should be empty)
        var locZeroQty = WMS.Domain.Entities.LocationEntity.Create(TenantId, WarehouseId, areaId, blockId, null, "Zero Qty Loc", 3, 1, 1);
        var invZero = WMS.Domain.Entities.InventoryAggregateRoot.InventoryItem.Create(
            TenantId, skuId, locZeroQty.Id, null, null, null, 0, 100, DateTime.UtcNow, null);

        db.Locations.AddRange(locEmpty, locOccupied, locZeroQty);
        db.InventoryItems.AddRange(invOccupied, invZero);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLocationsWithOccupancyQueryHandler(CreateUnitOfWork(db));
        var query = new GetLocationsWithOccupancyQuery(WarehouseId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);

        var mappedEmpty = result.Single(x => x.Id == locEmpty.Id);
        mappedEmpty.OccupancyStatus.Should().Be("empty");

        var mappedOccupied = result.Single(x => x.Id == locOccupied.Id);
        mappedOccupied.OccupancyStatus.Should().Be("occupied");

        var mappedZero = result.Single(x => x.Id == locZeroQty.Id);
        mappedZero.OccupancyStatus.Should().Be("empty");
    }
}
