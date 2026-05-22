using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.UpdateSku;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class UpdateSkuCommandHandlerTests : BaseSkuHandlerTest
{
    #region UpdateSkuCommandHandler

    [Fact]
    public async Task Update_WhenAllNullableFieldsAreNull_OnlyTouchesEntity()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone", description: "Desc", price: 10, updatedAt: BaseTime);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, null, null, null, null), TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Phone");
        updated.Description.Should().Be("Desc");
        updated.Price.Should().Be(10);
        updated.UpdatedAt.Should().BeAfter(BaseTime);
    }

    [Fact]
    public async Task Update_WhenSkuDoesNotExist_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, Guid.NewGuid(), null, "Phone", null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Update_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantB, "SKU-001", "Phone");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, null, "Phone", null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Update_WhenSkuIsDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        sku.MarkDeleted();
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, null, "Phone", null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task Update_WhenCategoryExistsInSameTenant_UpdatesCategoryId()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantA, "Electronics");
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Categories.Add(category);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, category.Id, null, null, null), TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Update_WhenCategoryDoesNotExist_ThrowsInvalidCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, Guid.NewGuid(), null, null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "INVALID_CATEGORY" && x.Message == "Category not found");
    }

    [Fact]
    public async Task Update_WhenCategoryBelongsToOtherTenant_ThrowsInvalidCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantB, "Electronics");
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Categories.Add(category);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, category.Id, null, null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "INVALID_CATEGORY" && x.Message == "Category not found");
    }

    [Fact]
    public async Task Update_WhenCategoryIsDeleted_ThrowsInvalidCategory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantA, "Electronics");
        category.MarkDeleted();
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Categories.Add(category);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, category.Id, null, null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "INVALID_CATEGORY" && x.Message == "Category not found");
    }

    [Fact]
    public async Task Update_WhenScalarFieldsSupplied_TrimsAndUpdatesFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, null, "  Tablet  ", "  New desc  ", 25.5m), TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Tablet");
        updated.Description.Should().Be("New desc");
        updated.Price.Should().Be(25.5m);
    }

    [Fact]
    public async Task Update_WhenDescriptionIsWhitespace_SetsDescriptionToNull()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone", description: "Desc");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(new UpdateSkuCommand(TenantA, sku.Id, null, null, "   ", null), TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateValidator_WhenNameIsWhitespace_ReturnsEnglishError()
    {
        var result = new UpdateSkuCommandValidator().Validate(new UpdateSkuCommand(TenantA, Guid.NewGuid(), null, "   ", null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU name cannot be empty");
    }

    [Fact]
    public void UpdateValidator_WhenPriceIsNegative_ReturnsEnglishError()
    {
        var result = new UpdateSkuCommandValidator().Validate(new UpdateSkuCommand(TenantA, Guid.NewGuid(), null, null, null, -1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU price cannot be negative");
    }

    [Fact]
    public void UpdateValidator_WhenNullableFieldsAreNullAndPriceIsZero_Passes()
    {
        var result = new UpdateSkuCommandValidator().Validate(new UpdateSkuCommand(TenantA, Guid.NewGuid(), null, null, null, 0));

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Helper Methods


    private static UpdateSkuCommandHandler CreateUpdateHandler(WmsDbContext db)
    {
        return new UpdateSkuCommandHandler(CreateUnitOfWork(db));
    }

    #endregion
}