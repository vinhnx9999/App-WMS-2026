using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.Commands.DeleteCategory;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Product;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class DeleteCategoryCommandHandlerTests : BaseCategoryHandlerTest
{
    private static DeleteCategoryCommandHandler CreateHandler(WmsDbContext db)
    {
        return new DeleteCategoryCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithNoActiveProducts_SoftDeletesCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteCategoryCommand(TenantA, category.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var deleted = await db.Set<Category>()
            .IgnoreQueryFilters() // Soft-deleted items are excluded by default global query filters
            .FirstOrDefaultAsync(x => x.Id == category.Id, TestContext.Current.CancellationToken);

        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteCategoryCommand(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Code.Should().Be("CATEGORY_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithActiveProductsReferencingCategory_ThrowsCategoryInUseException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware");
        db.Set<Category>().Add(category);

        // Add a product referencing this category
        var product = Product.Create(TenantA, "PRD-001", "Product A", "Description A", category.Id);
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteCategoryCommand(TenantA, category.Id);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("CATEGORY_IN_USE");
        exception.Which.Message.Should().Be("Cannot delete category because it is associated with active products.");
    }
}
