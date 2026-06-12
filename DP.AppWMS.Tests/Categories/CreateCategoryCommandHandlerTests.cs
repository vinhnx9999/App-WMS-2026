using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.Commands.CreateCategory;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class CreateCategoryCommandHandlerTests : BaseCategoryHandlerTest
{
    private static CreateCategoryCommandHandler CreateHandler(WmsDbContext db)
    {
        return new CreateCategoryCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateCategoryCommand(TenantA, "  Electronics  ", "Test description");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(TenantA);
        result.Name.Should().Be("Electronics"); // Trimmed
        result.Description.Should().Be("Test description");
        result.CreatedAt.Should().BeAfter(DateTime.MinValue);

        var savedCategory = await db.Set<Category>().FirstOrDefaultAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);
        savedCategory.Should().NotBeNull();
        savedCategory!.Name.Should().Be("Electronics");
        savedCategory.Description.Should().Be("Test description");
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

        var handler = CreateHandler(db);
        var command = new CreateCategoryCommand(TenantA, invalidName!, "Test description");

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("VALIDATION_FAILED");
        exception.Which.Message.Should().Be("Category name is required.");
    }

    [Fact]
    public async Task Handle_WithNameExceedingLimit_ThrowsValidationException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var longName = new string('A', 201);
        var command = new CreateCategoryCommand(TenantA, longName, "Test description");

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("VALIDATION_FAILED");
        exception.Which.Message.Should().Be("Category name must not exceed 200 characters.");
    }

    [Fact]
    public async Task Handle_WithDescriptionExceedingLimit_ThrowsValidationException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var longDescription = new string('A', 501);
        var command = new CreateCategoryCommand(TenantA, "Electronics", longDescription);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be("VALIDATION_FAILED");
        exception.Which.Message.Should().Be("Category description must not exceed 500 characters.");
    }
}
