using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Warehouse.Queries.WarehouseLookup;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Warehouses;

public sealed class WarehouseLookupTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

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
        return new UnitOfWork(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyWarehousesForRequestedTenantAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var whTenantA = new Warehouse(TenantA, "Warehouse A", "WHA");
        var whTenantB = new Warehouse(TenantB, "Warehouse B", "WHB");

        db.Warehouses.AddRange(whTenantA, whTenantB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(whTenantA.Id);
        result[0].Code.Should().Be("WHA");
        result[0].Name.Should().Be("Warehouse A");
    }

    [Fact]
    public async Task Handle_ExcludesSoftDeletedWarehousesAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var whActive = new Warehouse(TenantA, "Active Warehouse", "WHA");
        var whDeleted = new Warehouse(TenantA, "Deleted Warehouse", "WHD");
        whDeleted.MarkDeleted("admin");

        db.Warehouses.AddRange(whActive, whDeleted);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(whActive.Id);
    }

    [Fact]
    public async Task Handle_ReturnsWarehousesOrderedByNameAscendingAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var whC = new Warehouse(TenantA, "C Warehouse", "WHC");
        var whA = new Warehouse(TenantA, "A Warehouse", "WHA");
        var whB = new Warehouse(TenantA, "B Warehouse", "WHB");

        db.Warehouses.AddRange(whC, whA, whB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("A Warehouse");
        result[1].Name.Should().Be("B Warehouse");
        result[2].Name.Should().Be("C Warehouse");
    }

    [Fact]
    public async Task Handle_WhenNoWarehousesExist_ReturnsEmptyListAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlySpecificFieldsAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var warehouse = new Warehouse(TenantA, "Name Test", "Code Test", "Address Test");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(warehouse.Id);
        result[0].Code.Should().Be("Code Test");
        result[0].Name.Should().Be("Name Test");
    }

    [Fact]
    public async Task Handle_WhenTenantHasMultipleActiveWarehouses_ReturnsAllAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var wh1 = new Warehouse(TenantA, "WH 1", "WH1");
        var wh2 = new Warehouse(TenantA, "WH 2", "WH2");
        var wh3 = new Warehouse(TenantA, "WH 3", "WH3");

        db.Warehouses.AddRange(wh1, wh2, wh3);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(TenantA);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithEmptyTenantId_ReturnsEmptyListAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var wh = new Warehouse(TenantA, "Tenant WH", "TWH");
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new WarehouseLookupQueryHandler(CreateUnitOfWork(db));
        var query = new WarehouseLookupQuery(Guid.Empty);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}
