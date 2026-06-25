using FluentAssertions;
using WMS.Application.Skus;
using WMS.Domain.Entities.ProductAggregateRoot;

namespace DP.AppWMS.Tests.Skus;

public class SkuImportValidationHelperTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task ValidateSingleRowAsync_WhenProductCodeMissing_ShouldReturnProductCodeRequired()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "",
            SkuCode = "SKU-01",
            Name = "Sku Name",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_CODE_REQUIRED");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenProductNotFound_ShouldReturnProductNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "NON-EXISTENT",
            SkuCode = "SKU-01",
            Name = "Sku Name",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenSkuNameMissing_ShouldReturnSkuNameRequired()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-01", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "PROD-01",
            SkuCode = "SKU-01",
            Name = "",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("SKU_NAME_REQUIRED");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenReferencePriceNegative_ShouldReturnInvalidReferencePrice()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-01", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "PROD-01",
            SkuCode = "SKU-01",
            Name = "Sku Name",
            ReferencePrice = -10m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_REFERENCE_PRICE");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenSkuDuplicatedInImport_ShouldReturnDuplicateSkuInImport()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-01", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 2,
            ProductCode = "PROD-01",
            SkuCode = "SKU-01",
            Name = "Sku Name 2",
            ReferencePrice = 100m
        };
        var other = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "PROD-01",
            SkuCode = "sku-01", // case-insensitive check
            Name = "Sku Name 1",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { other, target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SKU_IN_IMPORT");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenSkuExistsInDatabase_ShouldReturnDuplicateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-01", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add a SKU to the database
        await AddTestSku(db, TenantA, "SKU-01", "Existing SKU");

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "PROD-01",
            SkuCode = "SKU-01",
            Name = "Sku Name",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SKU");
    }

    [Fact]
    public async Task ValidateSingleRowAsync_WhenRowIsValid_ShouldReturnValid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-01", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var helper = new SkuImportValidationHelper(CreateUnitOfWork(db));
        var target = new SkuImportRowValidationInput
        {
            RowNumber = 1,
            ProductCode = "PROD-01",
            SkuCode = "SKU-01",
            Name = "Sku Name",
            ReferencePrice = 100m
        };

        var result = await helper.ValidateSingleRowAsync(TenantA, target, new[] { target }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.ProductId.Should().Be(product.Id);
    }
}
