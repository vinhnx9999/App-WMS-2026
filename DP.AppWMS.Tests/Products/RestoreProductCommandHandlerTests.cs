using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Products.Commands.RestoreProduct;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace DP.AppWMS.Tests.Products;

public sealed class RestoreProductCommandHandlerTests : BaseProductHandlerTest
{
    [Fact]
    public async Task Handle_DeletedProduct_RestoresSuccessfully()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product");
        product.Delete();
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RestoreProductCommandHandler(CreateUnitOfWork(db));
        var command = new RestoreProductCommand(TenantA, product.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var restored = await db.Products.FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        restored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeletedProduct_SKUsRemainDeleted()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product");
        product.Delete();
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sku = Sku.Create(TenantA, product.Id, "SKU-001", "SKU", null, null, 10m);
        sku.Delete();
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RestoreProductCommandHandler(CreateUnitOfWork(db));
        var command = new RestoreProductCommand(TenantA, product.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Verify SKU is still deleted
        var savedSku = await db.Skus.IgnoreQueryFilters().FirstAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        savedSku.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonDeletedProduct_ThrowsProductNotDeleted()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product");
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RestoreProductCommandHandler(CreateUnitOfWork(db));
        var command = new RestoreProductCommand(TenantA, product.Id);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "PRODUCT_NOT_DELETED" && x.Message == "Only deleted products can be restored.");
    }

    [Fact]
    public async Task Handle_MissingProduct_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new RestoreProductCommandHandler(CreateUnitOfWork(db));
        var command = new RestoreProductCommand(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND");
    }
}
