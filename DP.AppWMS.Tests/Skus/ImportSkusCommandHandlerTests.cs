using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.ImportSkus;
using WMS.Application.Product.Skus.DTOs;
using WMS.Domain.Entities;
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
    public async Task Handle_InsertWithValidRows_ShouldCreateSkus()
    {
        await using var context = CreateContext();
        var category = await SeedMasterDataAsync(context);
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

        var sku = await context.Skus
            .Include(x => x.SkuSpecifications)
            .Include(x => x.SkuUnitOfMeasures)
            .SingleAsync();

        sku.TenantId.Should().Be(TenantId);
        sku.SkuCode.Should().Be("SKU-001");
        sku.Name.Should().Be("SKU One");
        sku.GoodsNature.Should().Be("Normal");
        sku.CategoryId.Should().Be(category.Id);
        sku.SkuSpecifications.Should().ContainSingle();
        sku.SkuUnitOfMeasures.Should().ContainSingle(x => x.ConversionFactor == 1);
    }

    [Fact]
    public async Task Handle_WhenSkuCodeExists_ShouldReturnRowError()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        context.Skus.Add(new Sku { TenantId = TenantId, SkuCode = "SKU-001", Name = "Existing" });
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
    public async Task Handle_WhenSpecificationMissingAndAutoCreateDisabled_ShouldReturnRowError()
    {
        await using var context = CreateContext();
        await SeedCategoryAndUomAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "SPECIFICATION_NOT_FOUND" && x.Field == nameof(ImportSkuRowInput.SpecificationCode));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUnitOfMeasureMissingAndAutoCreateDisabled_ShouldReturnRowError()
    {
        await using var context = CreateContext();
        await SeedCategoryAndSpecificationAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ImportSkusCommand(
            TenantId,
            [ValidRow()],
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        result.InsertedRows.Should().Be(0);
        result.FailedRows.Should().Be(1);
        result.Errors.Should().ContainSingle(x => x.Code == "UNIT_OF_MEASURE_NOT_FOUND" && x.Field == nameof(ImportSkuRowInput.UnitOfMeasureCode));
        context.Skus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAutoCreateMasterDataEnabled_ShouldCreateMasterDataAndSkus()
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
    public async Task Handle_WhenInsertFails_ShouldRollbackTransaction()
    {
        await using var context = CreateContext();
        await SeedMasterDataAsync(context);
        var handler = CreateHandler(context);

        var rows = Enumerable.Range(1, SkuImportDefaults.BatchSize + 1)
            .Select(i => ValidRow(i, $"SKU-{i:D3}"))
            .ToList();

        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_Test_Skus_SkuCode ON Skus (SkuCode);");

        var act = () => handler.Handle(new ImportSkusCommand(
            TenantId,
            rows,
            ImportSkuMode.Insert,
            false), CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        context.Skus.Should().BeEmpty();
    }

    private static WmsDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new WmsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static ImportSkusCommandHandler CreateHandler(WmsDbContext context)
    {
        return new ImportSkusCommandHandler(new UnitOfWork(context, NullLogger<UnitOfWork>.Instance));
    }

    private static ImportSkuRowInput ValidRow(
        int rowNumber = 1,
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
        context.Specifications.Add(new SkuAttribute { TenantId = TenantId, Code = "SPEC-A", Name = "Spec A" });
        context.UnitOfMeasures.Add(new UnitOfMeasure { TenantId = TenantId, Code = "PCS", Name = "Pieces" });
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task SeedSpecificationAndUomAsync(WmsDbContext context)
    {
        context.Specifications.Add(new SkuAttribute { TenantId = TenantId, Code = "SPEC-A", Name = "Spec A" });
        context.UnitOfMeasures.Add(new UnitOfMeasure { TenantId = TenantId, Code = "PCS", Name = "Pieces" });
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoryAndUomAsync(WmsDbContext context)
    {
        await SeedCategoryAsync(context);
        context.UnitOfMeasures.Add(new UnitOfMeasure { TenantId = TenantId, Code = "PCS", Name = "Pieces" });
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoryAndSpecificationAsync(WmsDbContext context)
    {
        await SeedCategoryAsync(context);
        context.Specifications.Add(new SkuAttribute { TenantId = TenantId, Code = "SPEC-A", Name = "Spec A" });
        await context.SaveChangesAsync();
    }

    private static async Task<Category> SeedCategoryAsync(WmsDbContext context)
    {
        var category = new Category { TenantId = TenantId, Name = "Electronics" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }
}
