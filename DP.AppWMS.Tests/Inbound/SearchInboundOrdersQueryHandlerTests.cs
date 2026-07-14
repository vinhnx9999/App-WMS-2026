using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Common.DynamicSearch;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Queries.SearchInboundOrders;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public sealed class SearchInboundOrdersQueryHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
    public async Task Handle_WithDynamicQueries_FiltersCorrectlyAsync()
    {
        // Arrange
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed Supplier and Sku
        var supplier = Supplier.Create(TenantA, "Code A", "Supplier A", address: "Address A");
        db.Set<Supplier>().Add(supplier);

        var sku = Sku.Create(TenantA, Guid.NewGuid(), "SKU001", "Sku A", "Nature A", "Description A", 10.0m, "Barcode A", 0, 100);
        db.Set<Sku>().Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Seed Inbound Orders
        var order1 = InboundOrder.Create(TenantA, "PO-001", DateOnly.FromDateTime(DateTime.UtcNow), "Notes 1");
        order1.AddItem(sku.Id, 10, supplier.Id);
        db.Set<InboundOrder>().Add(order1);

        var order2 = InboundOrder.Create(TenantA, "PO-002", DateOnly.FromDateTime(DateTime.UtcNow), "Notes 2");
        order2.AddItem(sku.Id, 5, supplier.Id);
        order2.CompleteOrder(); // Changes status to Completed
        db.Set<InboundOrder>().Add(order2);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uow = CreateUnitOfWork(db);
        var handler = new SearchInboundOrdersQueryHandler(uow);

        // Create JSON queries string to filter by Status = Completed
        var queriesList = new List<SearchObject>
        {
            new SearchObject
            {
                Name = "Status",
                Operator = Operators.Equal,
                Text = "Completed"
            }
        };
        var query = new SearchInboundOrdersQuery(
            TenantA,
            queriesList,
            1,
            10);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].OrderNumber.Should().Be("PO-002");
    }
}
