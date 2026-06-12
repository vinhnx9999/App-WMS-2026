using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.Commands.RestoreCategory;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class RestoreCategoryCommandHandlerTests : BaseCategoryHandlerTest
{
    private static RestoreCategoryCommandHandler CreateHandler(WmsDbContext db)
    {
        return new RestoreCategoryCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithSoftDeletedId_RestoresCategorySuccessfully()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware");
        category.MarkDeleted("user");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new RestoreCategoryCommand(TenantA, category.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var restored = await db.Set<Category>().FirstOrDefaultAsync(x => x.Id == category.Id, TestContext.Current.CancellationToken);
        restored.Should().NotBeNull();
        restored!.IsDeleted.Should().BeFalse();
        restored.DeletedAt.Should().BeNull();
        restored.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithActiveId_ThrowsBadRequestException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = Category.Create(TenantA, "Hardware");
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new RestoreCategoryCommand(TenantA, category.Id);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("CATEGORY_NOT_DELETED");
        exception.Which.Message.Should().Be("Only deleted categories can be restored.");
    }
}
