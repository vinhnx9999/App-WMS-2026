using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.DynamicSearch;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Queries.SearchInboundReceipts;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public class SearchInboundReceiptsTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;

    public SearchInboundReceiptsTests()
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
    public async Task Handle_ShouldReturnPagedInboundReceipts()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        var tenantId = Guid.NewGuid();

        var warehouse = new Warehouse(tenantId, "Warehouse A", "WHA");
        db.Warehouses.Add(warehouse);

        var inboundOrder = InboundOrder.Create(tenantId, "PO-SEARCH-100", null, null);
        db.InboundOrders.Add(inboundOrder);
        await db.SaveChangesAsync();

        var receipt = new InboundReceipt(tenantId, "REC-SEARCH-200", inboundOrder.Id, warehouse.Id);
        receipt.AddItem(Guid.NewGuid(), 10, 10, "Test Notes");
        db.InboundReceipts.Add(receipt);
        await db.SaveChangesAsync();

        var handler = new SearchInboundReceiptsQueryHandler(uow);
        var query = new SearchInboundReceiptsQuery(
            tenantId,
            new List<SearchObject>(),
            Page: 1,
            Limit: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.ReceiptNumber.Should().Be("REC-SEARCH-200");
        item.InboundOrderNumber.Should().Be("PO-SEARCH-100");
        item.WarehouseName.Should().Be("Warehouse A");
        item.ItemsCount.Should().Be(1);
    }
}
