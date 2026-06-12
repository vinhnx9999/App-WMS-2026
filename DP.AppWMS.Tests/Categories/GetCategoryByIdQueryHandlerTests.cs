using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Categories.Queries.GetCategoryById;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class GetCategoryByIdQueryHandlerTests : BaseCategoryHandlerTest
{
    private static GetCategoryByIdQueryHandler CreateHandler(WmsDbContext db)
    {
        return new GetCategoryByIdQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCategoryDetails()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware", "Tools and materials");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCategoryByIdQuery(TenantA, category.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Id.Should().Be(category.Id);
        result.TenantId.Should().Be(TenantA);
        result.Name.Should().Be("Hardware");
        result.Description.Should().Be("Tools and materials");
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCategoryByIdQuery(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Code.Should().Be("CATEGORY_NOT_FOUND");
        exception.Which.Message.Should().Be("Category not found.");
    }

    [Fact]
    public async Task Handle_WithSoftDeletedId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware", "Tools and materials");
        category.MarkDeleted("user");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCategoryByIdQuery(TenantA, category.Id);

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Code.Should().Be("CATEGORY_NOT_FOUND");
        exception.Which.Message.Should().Be("Category not found.");
    }

    [Fact]
    public async Task Handle_WithCrossTenantId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantB, "Hardware", "Tools and materials");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCategoryByIdQuery(TenantA, category.Id); // Querying Tenant B's category with Tenant A credentials

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Code.Should().Be("CATEGORY_NOT_FOUND");
        exception.Which.Message.Should().Be("Category not found.");
    }
}
