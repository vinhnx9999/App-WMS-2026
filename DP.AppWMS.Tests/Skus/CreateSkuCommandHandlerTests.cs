using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.CreateSku;
using WMS.Application.Product.Skus.DTOs;
using WMS.Application.Product.Skus.Validators;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class CreateSkuCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenExplicitCategoryExists_ShouldCreateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantA, "electronics");
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "SKU-001", category.Id, null, null, null),
            TestContext.Current.CancellationToken);

        result.CategoryId.Should().Be(category.Id);
        result.CategoryName.Should().Be(category.Name);
        result.Name.Should().BeEmpty();
        result.Description.Should().BeNull();
        result.Price.Should().Be(0m);
        db.Skus.Should().ContainSingle(x => x.Id == result.Id);
    }

    [Fact]
    public async Task Handle_WhenCategoryOmittedAndDefaultExists_ShouldUseDefaultCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var defaultCategory = CreateCategory(TenantA, SkuDefaults.DefaultCategoryName);
        defaultCategory.Slug = SkuDefaults.DefaultCategorySlug;
        db.Categories.Add(defaultCategory);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "SKU-001", null, "Test SKU", "Test description", 10m),
            TestContext.Current.CancellationToken);

        result.CategoryId.Should().Be(defaultCategory.Id);
        result.CategoryName.Should().Be(SkuDefaults.DefaultCategoryName);
        db.Categories.Should().ContainSingle(x => x.Name == SkuDefaults.DefaultCategoryName);
    }

    [Fact]
    public async Task Handle_WhenCategoryOmittedAndDefaultMissing_ShouldCreateDefaultCategoryAndSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "SKU-001", null, null, null, null),
            TestContext.Current.CancellationToken);

        var defaultCategory = await db.Categories.SingleAsync(x => x.Name == SkuDefaults.DefaultCategoryName, TestContext.Current.CancellationToken);
        defaultCategory.Slug.Should().Be(SkuDefaults.DefaultCategorySlug);
        defaultCategory.TenantId.Should().Be(TenantA);
        result.CategoryId.Should().Be(defaultCategory.Id);
        result.Name.Should().BeEmpty();
        result.Description.Should().BeNull();
        result.Price.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WhenDuplicateSkuCodeSameTenantDifferentCase_ShouldThrowDuplicate()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var defaultCategory = CreateCategory(TenantA, SkuDefaults.DefaultCategoryName);
        db.Categories.Add(defaultCategory);
        db.Skus.Add(CreateSku(TenantA, "ABC", "Existing SKU"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "abc", null, "Test SKU", null, 10m),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "DUPLICATE_SKU" && x.Message == "SKU code already exists");
    }

    [Fact]
    public async Task Handle_WhenSameSkuCodeDifferentTenant_ShouldCreateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var defaultCategory = CreateCategory(TenantA, SkuDefaults.DefaultCategoryName);
        db.Categories.Add(defaultCategory);
        db.Skus.Add(CreateSku(TenantB, "ABC", "Other tenant SKU"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "abc", null, "Test SKU", null, 10m),
            TestContext.Current.CancellationToken);

        result.SkuCode.Should().Be("abc");
        result.TenantId.Should().Be(TenantA);
    }

    [Fact]
    public async Task Handle_WhenSoftDeletedSkuHasSameCode_ShouldCreateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var defaultCategory = CreateCategory(TenantA, SkuDefaults.DefaultCategoryName);
        var deletedSku = CreateSku(TenantA, "ABC", "Deleted SKU");
        deletedSku.MarkDeleted();
        db.Categories.Add(defaultCategory);
        db.Skus.Add(deletedSku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "abc", null, "Test SKU", null, 10m),
            TestContext.Current.CancellationToken);

        result.SkuCode.Should().Be("abc");
        db.Skus.Count(x => x.TenantId == TenantA).Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherTenant_ShouldThrowInvalidCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantB, "external");
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "SKU-001", category.Id, "Test SKU", null, 10m),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "INVALID_CATEGORY" && x.Message == "Category not found");
    }

    [Fact]
    public async Task Handle_WhenCategoryIsSoftDeleted_ShouldThrowInvalidCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantA, "deleted");
        category.MarkDeleted();
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, "SKU-001", category.Id, "Test SKU", null, 10m),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "INVALID_CATEGORY" && x.Message == "Category not found");
    }

    [Fact]
    public void Validator_WhenSkuCodeWhitespace_ShouldFail()
    {
        var result = new CreateSkuCommandValidator().Validate(new CreateSkuCommand(TenantA, "   ", null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU code is required");
    }

    [Fact]
    public void Validator_WhenPriceNegative_ShouldFail()
    {
        var result = new CreateSkuCommandValidator().Validate(new CreateSkuCommand(TenantA, "SKU-001", null, null, null, -1m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU price cannot be negative");
    }

    private static CreateSkuCommandHandler CreateCreateHandler(WmsDbContext db)
    {
        return new CreateSkuCommandHandler(CreateUnitOfWork(db));
    }
}
