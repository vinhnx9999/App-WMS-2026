using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Queries.GetInboundReceiptById;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inbound;

public class GetInboundReceiptByIdTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;

    public GetInboundReceiptByIdTests()
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
    public async Task Handle_ShouldReturnDetailedInboundReceiptWithSkuAndSupplierNames()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        var tenantId = Guid.NewGuid();

        var warehouse = new Warehouse(tenantId, "Warehouse B", "WHB");
        db.Warehouses.Add(warehouse);

        var inboundOrder = InboundOrder.Create(tenantId, "PO-DETAIL-100", null, null);
        db.InboundOrders.Add(inboundOrder);

        var supplier = Supplier.Create(tenantId, "SUPB", "Supplier B");
        db.Suppliers.Add(supplier);

        var sku = Sku.Create(tenantId, Guid.NewGuid(), "SKU-DETAIL-1", "Sku Detail One", null, null, 10.0m, null, 0, 100);
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var receipt = new InboundReceipt(tenantId, "REC-DETAIL-200", inboundOrder.Id, warehouse.Id);
        var expiry = new DateOnly(2026, 12, 31);
        receipt.AddItem(sku.Id, 20, 15, "Detail notes", supplier.Id, expiry, "SN12345", "LOT999");
        db.InboundReceipts.Add(receipt);
        await db.SaveChangesAsync();

        var handler = new GetInboundReceiptByIdQueryHandler(uow);
        var query = new GetInboundReceiptByIdQuery(tenantId, receipt.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptNumber.Should().Be("REC-DETAIL-200");
        result.InboundOrderNumber.Should().Be("PO-DETAIL-100");
        result.WarehouseName.Should().Be("Warehouse B");
        result.Items.Should().HaveCount(1);

        var item = result.Items.First();
        item.SkuId.Should().Be(sku.Id);
        item.SkuCode.Should().Be("SKU-DETAIL-1");
        item.SkuName.Should().Be("Sku Detail One");
        item.ExpectedQuantity.Should().Be(20);
        item.ReceivedQuantity.Should().Be(15);
        item.Notes.Should().Be("Detail notes");
        item.SupplierId.Should().Be(supplier.Id);
        item.SupplierName.Should().Be("Supplier B");
        item.ExpiryDate.Should().Be(expiry);
        item.SerialNumber.Should().Be("SN12345");
        item.LotNumber.Should().Be("LOT999");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenReceiptDoesNotExist()
    {
        // Arrange
        var (connection, db, uow) = await SetupInMemoryDbAsync();
        var handler = new GetInboundReceiptByIdQueryHandler(uow);
        var query = new GetInboundReceiptByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404);
    }
}
