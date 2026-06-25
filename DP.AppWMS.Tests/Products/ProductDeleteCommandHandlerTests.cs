using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Products.Commands.DeleteProduct;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Products;

public sealed class ProductDeleteCommandHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Handle_WhenProductHasActiveSku_ShouldThrowConflict()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Skus.Add(Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 0m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateHandler(db).Handle(
            new DeleteProductCommand(TenantA, product.Id),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x =>
                x.StatusCode == 409
                && x.Code == "CONFLICT"
                && x.Message == "Product cannot be deleted while active SKUs exist.");
    }

    [Fact]
    public async Task Handle_WhenProductHasOnlyDeletedSku_ShouldDeleteProduct()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "Test SKU", null, null, 0m);
        sku.Delete();
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateHandler(db).Handle(
            new DeleteProductCommand(TenantA, product.Id),
            TestContext.Current.CancellationToken);

        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSkuBelongsToOtherTenant_ShouldDeleteProduct()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Products.Add(product);
        db.Skus.Add(Sku.Create(TenantB, product.Id, "SKU-001", "Other SKU", null, null, 0m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateHandler(db).Handle(
            new DeleteProductCommand(TenantA, product.Id),
            TestContext.Current.CancellationToken);

        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowProductNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateHandler(db).Handle(
            new DeleteProductCommand(TenantA, Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND");
    }

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

    private static DeleteProductCommandHandler CreateHandler(WmsDbContext db)
    {
        return new DeleteProductCommandHandler(new UnitOfWork(db, NullLogger<UnitOfWork>.Instance));
    }
}
