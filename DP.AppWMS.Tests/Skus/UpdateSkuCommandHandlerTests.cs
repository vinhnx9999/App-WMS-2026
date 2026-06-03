using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.UpdateSku;
using WMS.Application.Product.Skus.Validators;
using WMS.Domain.Entities.Product;
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
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", "Electronic", "Desc", 10m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, sku.Id),
            TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Phone");
        updated.Description.Should().Be("Desc");
        updated.ReferencePrice.Should().Be(10m);
    }

    [Fact]
    public async Task Update_WhenSkuDoesNotExist_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, Guid.NewGuid(), Name: "Phone"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Update_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantB, "PROD-001", "Other Tenant Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantB, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, sku.Id, Name: "Phone"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Update_WhenSkuIsDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        sku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, sku.Id, Name: "Phone"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task Update_WhenScalarFieldsSupplied_TrimsAndUpdatesFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, sku.Id, "  Tablet  ", "  Electronic  ", "  New desc  ", 25.5m),
            TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Tablet");
        updated.GoodsNature.Should().Be("Electronic");
        updated.Description.Should().Be("New desc");
        updated.ReferencePrice.Should().Be(25.5m);
    }

    [Fact]
    public async Task Update_WhenDescriptionIsWhitespace_SetsDescriptionToNull()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, "Desc", 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateUpdateHandler(db).Handle(
            new UpdateSkuCommand(TenantA, sku.Id, Description: "   "),
            TestContext.Current.CancellationToken);

        var updated = await db.Skus.SingleAsync(x => x.Id == sku.Id, TestContext.Current.CancellationToken);
        updated.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateValidator_WhenNameIsWhitespace_ReturnsEnglishError()
    {
        var result = new UpdateSkuCommandValidator().Validate(
            new UpdateSkuCommand(TenantA, Guid.NewGuid(), Name: "   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU name cannot be empty");
    }

    [Fact]
    public void UpdateValidator_WhenPriceIsNegative_ReturnsEnglishError()
    {
        var result = new UpdateSkuCommandValidator().Validate(
            new UpdateSkuCommand(TenantA, Guid.NewGuid(), Price: -1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorMessage == "SKU price cannot be negative");
    }

    [Fact]
    public void UpdateValidator_WhenNullableFieldsAreNullAndPriceIsZero_Passes()
    {
        var result = new UpdateSkuCommandValidator().Validate(
            new UpdateSkuCommand(TenantA, Guid.NewGuid(), Price: 0));

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
