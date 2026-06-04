using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.ImportSkus;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class ImportSkusCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidateOnlyWithValidRows_ShouldNotInsertSkus()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.ValidateOnly,
            false), CancellationToken.None);

        result.TotalRows.Should().Be(1);
        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(0);
        result.Errors.Should().BeEmpty();
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InsertWithValidRows_ShouldCreateProductAndSkusThroughAggregate()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.TotalRows.Should().Be(1);
        result.InsertedRows.Should().Be(1);
        result.FailedRows.Should().Be(0);
        result.Errors.Should().BeEmpty();

        var product = await context.Set<Product>()
            .Include(x => x.Skus)
            .ThenInclude(x => x.Attributes)
            .Include(x => x.Skus)
            .ThenInclude(x => x.AllowedUnits)
            .SingleAsync();

        product.ProductCode.Should().Be("PROD-001");
        product.Skus.Should().ContainSingle();

        var sku = product.Skus.Single();
        sku.TenantId.Should().Be(TenantId);
        sku.ProductId.Should().Be(product.Id);
        sku.SkuCode.Should().Be("SKU-001");
        sku.Name.Should().Be("SKU One");
        sku.GoodsNature.Should().Be("Normal");
        sku.Attributes.Should().ContainSingle();
        sku.AllowedUnits.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenSkuCodeExists_ShouldReturnRowError()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var product = Product.Create(TenantId, "PROD-EXISTING", "Existing Product");
        context.Set<Product>().Add(product);
        await context.SaveChangesAsync();
        product.AddSku(TenantId, "SKU-001", "Existing", null, null, null);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "SKU_CODE_EXISTS" && x.Field == nameof(ImportSkuRowInput.SkuCode));
        context.Skus.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenProductCodeMissing_ShouldReturnRowValidationError()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow(productCode: " ")],
            ImportSkuMode.Insert,
            true), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "PRODUCT_CODE_REQUIRED" && x.Field == nameof(ImportSkuRowInput.ProductCode));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCategoryMissingAndAutoCreateDisabled_ShouldReturnRowError()
    {
        await using var context = CreateContext();
        await SeedSpecificationAndUomAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "CATEGORY_NOT_FOUND" && x.Field == nameof(ImportSkuRowInput.CategoryName));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAutoCreateMasterDataEnabled_ShouldCreateMasterDataProductAndSkus()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            true), CancellationToken.None);

        result.InsertedRows.Should().Be(1);
        result.FailedRows.Should().Be(0);
        result.Errors.Should().BeEmpty();
        context.Categories.Should().ContainSingle(x => x.Name == "Electronics");
        context.Specifications.Should().ContainSingle(x => x.Code == "SPEC-A");
        context.UnitOfMeasures.Should().ContainSingle(x => x.Code == "PCS");
        context.Set<Product>().Should().ContainSingle(x => x.ProductCode == "PROD-001");
        context.Skus.Should().ContainSingle(x => x.SkuCode == "SKU-001");
    }

    [Fact]
    public async Task Handle_WhenRequiredFieldMissing_ShouldReturnRowValidationError()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow(skuCode: " ")],
            ImportSkuMode.Insert,
            true), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "SKU_CODE_REQUIRED" && x.Field == nameof(ImportSkuRowInput.SkuCode));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenConversionFactorInvalid_ShouldReturnRowValidationError()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow(conversionFactor: 0)],
            ImportSkuMode.Insert,
            true), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "INVALID_CONVERSION_FACTOR" && x.Field == nameof(ImportSkuRowInput.ConversionFactor));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDifferentProductGroupsAndOneRowInvalid_ShouldImportValidGroupOnly()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [
                ValidRow(1, productCode: "PROD-A", skuCode: "SKU-A"),
                ValidRow(2, productCode: "PROD-B", skuCode: " ")
            ],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(1);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.RowNumber == 2 && x.Code == "SKU_CODE_REQUIRED");
        context.Set<Product>().Should().ContainSingle(x => x.ProductCode == "PROD-A");
        context.Skus.Should().ContainSingle(x => x.SkuCode == "SKU-A");
    }

    [Fact]
    public async Task Handle_WhenMoreThanOneBatchInSameProductGroup_ShouldImportAllRows()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var handler = CreateHandler(context);
        var rows = Enumerable.Range(1, SkuImportDefaults.BatchSize + 1)
            .Select(i => ValidRow(i, productCode: "PROD-BATCH", skuCode: $"SKU-{i:D3}"))
            .ToList();

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            rows,
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(SkuImportDefaults.BatchSize + 1);
        result.FailedRows.Should().Be(0);
        result.Errors.Should().BeEmpty();
        context.Set<Product>().Should().ContainSingle(x => x.ProductCode == "PROD-BATCH");
        context.Skus.Should().HaveCount(SkuImportDefaults.BatchSize + 1);
    }

    private static WmsDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new WmsDbContext(options, Mock.Of<ICurrentUser>());
        context.Database.EnsureCreated();
        return context;
    }

    private static ImportSkusCommandHandler CreateHandler(WmsDbContext context)
    {
        return new ImportSkusCommandHandler(new UnitOfWork(context, NullLogger<UnitOfWork>.Instance));
    }

    private static ImportSkuRowInput ValidRow(
        int rowNumber = 1,
        string? productCode = "PROD-001",
        string? skuCode = "SKU-001",
        string? skuName = "SKU One",
        string? categoryName = "Electronics",
        string? goodsNature = "Normal",
        string? specificationCode = "SPEC-A",
        string? unitOfMeasureCode = "PCS",
        decimal? conversionFactor = 1)
    {
        return new ImportSkuRowInput(
            rowNumber,
            productCode,
            skuCode,
            skuName,
            categoryName,
            goodsNature,
            specificationCode,
            unitOfMeasureCode,
            conversionFactor);
    }

    private static async Task<Category> SeedMasterDataAsync(WmsDbContext context)
    {
        var category = await SeedCategoryAsync(context);
        context.Specifications.Add(SkuAttribute.Create(TenantId, "SPEC-A", "Spec A"));
        context.UnitOfMeasures.Add(UnitOfMeasure.Create(TenantId, "PCS", "Pieces", null));
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task SeedSpecificationAndUomAsync(WmsDbContext context)
    {
        context.Specifications.Add(SkuAttribute.Create(TenantId, "SPEC-A", "Spec A"));
        context.UnitOfMeasures.Add(UnitOfMeasure.Create(TenantId, "PCS", "Pieces", null));
        await context.SaveChangesAsync();
    }

    private static async Task<Category> SeedCategoryAsync(WmsDbContext context)
    {
        var category = Category.Create(TenantId, "Electronics");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }
}
