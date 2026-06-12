using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.Commands.UpdateCategory;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class UpdateCategoryCommandHandlerTests : BaseCategoryHandlerTest
{
    private static UpdateCategoryCommandHandler CreateHandler(WmsDbContext db)
    {
        return new UpdateCategoryCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidRequest_UpdatesCategoryNameAndDescription()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Old Name", "Old Description");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateCategoryCommand(TenantA, category.Id, "  New Name  ", "New Description");

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = await db.Set<Category>().FirstOrDefaultAsync(x => x.Id == category.Id, TestContext.Current.CancellationToken);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name"); // Trimmed
        updated.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateCategoryCommand(TenantA, Guid.NewGuid(), "New Name", "New Description");

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Code.Should().Be("CATEGORY_NOT_FOUND");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithEmptyName_ThrowsValidationException(string? invalidName)
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateCategoryCommand(TenantA, category.Id, invalidName!, "New Description");

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("VALIDATION_FAILED");
    }
}
