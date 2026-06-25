using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Products.Queries.GetProductById;
using WMS.Domain.Entities.ProductAggregateRoot;

namespace DP.AppWMS.Tests.Products;

public sealed class GetProductByIdQueryHandlerTests : BaseProductHandlerTest
{
    [Fact]
    public async Task GetById_WhenProductExists_ReturnsDetailsWithCategoryInfo()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = await AddTestCategory(db, TenantA, "Hardware", TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PRD-001", "Hammer", "Steel hammer", category.Id);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductByIdQueryHandler(CreateUnitOfWork(db));
        var query = new GetProductByIdQuery(TenantA, product.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Id.Should().Be(product.Id);
        result.TenantId.Should().Be(TenantA);
        result.ProductCode.Should().Be("PRD-001");
        result.ProductName.Should().Be("Hammer");
        result.Description.Should().Be("Steel hammer");
        result.CategoryId.Should().Be(category.Id);
        result.CategoryName.Should().Be("Hardware");
    }

    [Fact]
    public async Task GetById_WhenProductHasNoCategory_ReturnsDetailsWithNullCategoryInfo()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-002", "Nails", "Box of nails", null);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductByIdQueryHandler(CreateUnitOfWork(db));
        var query = new GetProductByIdQuery(TenantA, product.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Id.Should().Be(product.Id);
        result.CategoryId.Should().BeNull();
        result.CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenProductIsDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PRD-DEL", "Deleted Item");
        product.Delete();
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductByIdQueryHandler(CreateUnitOfWork(db));
        var query = new GetProductByIdQuery(TenantA, product.Id);

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND" && x.Message == "Product not found.");
    }

    [Fact]
    public async Task GetById_WhenProductBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantB, "PRD-OTHER", "Other Tenant Product");
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductByIdQueryHandler(CreateUnitOfWork(db));
        var query = new GetProductByIdQuery(TenantA, product.Id);

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND");
    }
}
