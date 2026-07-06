using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.Queries.GetInboundById;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public sealed class GetInboundByIdQueryHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.TenantId).Returns(TenantA);
        currentUserMock.Setup(u => u.Email).Returns("test@example.com");

        return new WmsDbContext(options, currentUserMock.Object, Mock.Of<MediatR.IMediator>());
    }

    private static IUnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsExpectedResponseAndDetails()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Sku and Supplier (to allow adding item)
        var sku = Sku.Create(TenantA, Guid.NewGuid(), "SKU001", "Sku A", "Nature A", "Description A", 10.0m, "Barcode A", 0, 100);
        db.Set<Sku>().Add(sku);
        var supplier = Supplier.Create(TenantA, "SUPP01", "Supplier A", "Address A");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order = InboundOrder.Create(TenantA, "PO-001", DateOnly.FromDateTime(DateTime.UtcNow), "Some notes");
        order.AddItem(sku.Id, 10, supplier.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), "SN123", "LOT456");
        db.Set<InboundOrder>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundByIdQueryHandler(uow);
        var query = new GetInboundByIdQuery(TenantA, order.Id);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.OrderNumber.Should().Be("PO-001");
        result.Notes.Should().Be("Some notes");
        result.Items.Should().HaveCount(1);
        
        var itemDetail = result.Items[0];
        itemDetail.SkuId.Should().Be(sku.Id);
        itemDetail.SkuCode.Should().Be("SKU001");
        itemDetail.SkuName.Should().Be("Sku A");
        itemDetail.Quantity.Should().Be(10);
        itemDetail.SupplierId.Should().Be(supplier.Id);
        itemDetail.SupplierName.Should().Be("Supplier A");
        itemDetail.SerialNumber.Should().Be("SN123");
        itemDetail.LotNumber.Should().Be("LOT456");
    }

    [Fact]
    public async Task Handle_WithOrphanedOrDeletedMasterData_ReturnsNullForMasterFields()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed SKU (but it will be soft-deleted)
        var sku = Sku.Create(TenantA, Guid.NewGuid(), "SKU002", "Sku B", "Nature B", "Description B", 12.0m, "Barcode B", 0, 100);
        sku.Delete(); // Soft delete it
        db.Set<Sku>().Add(sku);

        // Seed Supplier (but it will be soft-deleted)
        var supplier = Supplier.Create(TenantA, "SUPP02", "Supplier B", "Address B");
        supplier.Delete(); // Soft delete it
        db.Set<Supplier>().Add(supplier);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order = InboundOrder.Create(TenantA, "PO-002", DateOnly.FromDateTime(DateTime.UtcNow), "Some notes");
        order.AddItem(sku.Id, 5, supplier.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "SN456", "LOT789");
        db.Set<InboundOrder>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundByIdQueryHandler(uow);
        var query = new GetInboundByIdQuery(TenantA, order.Id);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        
        var itemDetail = result.Items[0];
        itemDetail.SkuId.Should().Be(sku.Id);
        itemDetail.SkuCode.Should().BeNull();
        itemDetail.SkuName.Should().BeNull();
        itemDetail.SupplierId.Should().Be(supplier.Id);
        itemDetail.SupplierName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ThrowsAppExceptionNotFound()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundByIdQueryHandler(uow);
        var query = new GetInboundByIdQuery(TenantA, Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithWrongTenant_ThrowsAppExceptionNotFound()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var order = InboundOrder.Create(TenantA, "PO-001", DateOnly.FromDateTime(DateTime.UtcNow), "Some notes");
        db.Set<InboundOrder>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundByIdQueryHandler(uow);
        var query = new GetInboundByIdQuery(TenantB, order.Id); // Querying with TenantB

        // Act & Assert
        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithSoftDeletedOrder_ThrowsAppExceptionNotFound()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var order = InboundOrder.Create(TenantA, "PO-001", DateOnly.FromDateTime(DateTime.UtcNow), "Some notes");
        order.MarkDeleted(); // Soft delete using MarkDeleted()
        db.Set<InboundOrder>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new GetInboundByIdQueryHandler(uow);
        var query = new GetInboundByIdQuery(TenantA, order.Id);

        // Act & Assert
        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.Code == "NOT_FOUND");
    }
}
