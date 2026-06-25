using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Inventory.DTOs;
using WMS.Application.Inventory.Services;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Inventory;

public sealed class InventoryServiceTests
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

        return new WmsDbContext(options, Mock.Of<ICurrentUser>(u => u.TenantId == TenantA && u.Id == Guid.NewGuid()), Mock.Of<MediatR.IMediator>());
    }

    private static UnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    private static InventoryService CreateService(WmsDbContext db)
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(x => x.TenantId).Returns(TenantA);
        userMock.Setup(x => x.Id).Returns(Guid.NewGuid());
        return new InventoryService(CreateUnitOfWork(db), userMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesInventory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed references
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        var pallet = Pallet.Create(TenantA, "PAL-001");
        db.Set<Pallet>().Add(pallet);

        var supplier = Supplier.Create(TenantA, "SUP-001", "Supplier 1");
        db.Set<Supplier>().Add(supplier);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Seed target inventory item
        var item = InventoryItem.Create(
            TenantA,
            sku.Id,
            location.Id,
            supplier.Id,
            "SN-001",
            pallet.Id,
            100,
            10m,
            DateTime.UtcNow,
            null);
        db.Set<InventoryItem>().Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Prepare update request
        var newPutaway = DateTime.UtcNow.AddDays(1);
        var request = new UpdateInventoryRequest(
            SkuId: null, // Unchanged
            LocationId: null, // Unchanged
            SupplierId: null, // Unchanged
            SerialNumber: "SN-002",
            PalletId: null, // Unchanged
            Quantity: 150,
            UnitPrice: 12m,
            PutawayDate: newPutaway,
            ExpiryDate: null);

        var service = CreateService(db);
        await service.UpdateAsync(item.Id, request, TestContext.Current.CancellationToken);

        // Assert
        var updated = await db.Set<InventoryItem>().FirstAsync(x => x.Id == item.Id, TestContext.Current.CancellationToken);
        updated.SerialNumber.Should().Be("SN-002");
        updated.Quantity.Should().Be(150);
        updated.UnitPrice.Should().Be(12m);
    }

    [Fact]
    public async Task UpdateAsync_WhenInventoryNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);
        var request = new UpdateInventoryRequest(null, null, null, null, null, null, null, null, null);

        var act = () => service.UpdateAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_WhenSkuNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item = InventoryItem.Create(TenantA, sku.Id, location.Id, null, null, null, 100, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);
        var request = new UpdateInventoryRequest(SkuId: Guid.NewGuid(), null, null, null, null, null, null, null, null);

        var act = () => service.UpdateAsync(item.Id, request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message.Contains("SKU không tồn tại"));
    }

    [Fact]
    public async Task UpdateAsync_WhenLocationNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item = InventoryItem.Create(TenantA, sku.Id, location.Id, null, null, null, 100, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);
        var request = new UpdateInventoryRequest(null, LocationId: Guid.NewGuid(), null, null, null, null, null, null, null);

        var act = () => service.UpdateAsync(item.Id, request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message.Contains("Vị trí không tồn tại"));
    }

    [Fact]
    public async Task UpdateAsync_WhenPalletNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item = InventoryItem.Create(TenantA, sku.Id, location.Id, null, null, null, 100, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);
        var request = new UpdateInventoryRequest(null, null, null, null, PalletId: Guid.NewGuid(), null, null, null, null);

        var act = () => service.UpdateAsync(item.Id, request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message.Contains("Pallet không tồn tại"));
    }

    [Fact]
    public async Task UpdateAsync_WhenSupplierNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item = InventoryItem.Create(TenantA, sku.Id, location.Id, null, null, null, 100, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);
        var request = new UpdateInventoryRequest(null, null, SupplierId: Guid.NewGuid(), null, null, null, null, null, null);

        var act = () => service.UpdateAsync(item.Id, request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message.Contains("Nhà cung cấp không tồn tại"));
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateInventoryExists_ThrowsConflictException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed references
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 10m);
        db.Skus.Add(sku);

        var location = new LocationEntity(TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Loc-001");
        db.Set<LocationEntity>().Add(location);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Seed item 1
        var item1 = InventoryItem.Create(TenantA, sku.Id, location.Id, null, "SN-001", null, 100, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item1);

        // Seed item 2 (with SN-002)
        var item2 = InventoryItem.Create(TenantA, sku.Id, location.Id, null, "SN-002", null, 50, 10m, DateTime.UtcNow, null);
        db.Set<InventoryItem>().Add(item2);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(db);

        // Attempt to update item 2's SerialNumber to "SN-001", making it duplicate of item 1.
        var request = new UpdateInventoryRequest(null, null, null, SerialNumber: "SN-001", null, null, null, null, null);

        var act = () => service.UpdateAsync(item2.Id, request, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 409 && x.Code == "DUPLICATE");
    }
}
