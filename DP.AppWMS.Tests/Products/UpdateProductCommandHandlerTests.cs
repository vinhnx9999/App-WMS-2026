using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Products.Commands.UpdateProduct;
using WMS.Domain.Entities.ProductAggregateRoot;

namespace DP.AppWMS.Tests.Products;

public sealed class UpdateProductCommandHandlerTests : BaseProductHandlerTest
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesProductNameAndDescription()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Old Name", "Old Desc", null);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProductCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateProductCommand(TenantA, product.Id, "New Name", "New Desc", null);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = await db.Products.FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        updated.ProductName.Should().Be("New Name");
        updated.Description.Should().Be("New Desc");
        updated.ProductCode.Should().Be("PRD-001"); // Code is immutable
    }

    [Fact]
    public async Task Handle_ChangeCategoryId_UpdatesCategorySuccessfully()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var cat1 = await AddTestCategory(db, TenantA, "Category 1", TestContext.Current.CancellationToken);
        var cat2 = await AddTestCategory(db, TenantA, "Category 2", TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product", null, cat1.Id);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProductCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateProductCommand(TenantA, product.Id, "Product", null, cat2.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = await db.Products.FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        updated.CategoryId.Should().Be(cat2.Id);
    }

    [Fact]
    public async Task Handle_ClearCategoryWhenCategorized_ThrowsCategoryRequired()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var cat = await AddTestCategory(db, TenantA, "Category 1", TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product", null, cat.Id);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProductCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateProductCommand(TenantA, product.Id, "Product", null, null);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "CATEGORY_REQUIRED" && x.Message == "Category is required once product is categorized.");
    }

    [Fact]
    public async Task Handle_ClearCategoryWhenNotCategorized_Succeeds()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product", null, null);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProductCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateProductCommand(TenantA, product.Id, "Product Updated", null, null);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = await db.Products.FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        updated.ProductName.Should().Be("Product Updated");
        updated.CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InvalidCategoryId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-001", "Product", null, null);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProductCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateProductCommand(TenantA, product.Id, "Product", null, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "CATEGORY_NOT_FOUND");
    }
}
