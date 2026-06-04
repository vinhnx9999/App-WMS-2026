using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.CreateSku;
using WMS.Application.Product.Skus.Validators;
using WMS.Domain.Entities.Product;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class CreateSkuCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenProductExists_ShouldCreateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, "SKU-001", "Test SKU", "Electronic", "A test SKU", 10m),
            TestContext.Current.CancellationToken);

        result.ProductId.Should().Be(product.Id);
        result.ProductCode.Should().Be("PROD-001");
        result.ProductName.Should().Be("Test Product");
        result.SkuCode.Should().Be("SKU-001");
        result.Name.Should().Be("Test SKU");
        result.GoodsNature.Should().Be("Electronic");
        result.Description.Should().Be("A test SKU");
        result.ReferencePrice.Should().Be(10m);
        db.Skus.Should().ContainSingle(x => x.Id == result.Id);
    }

    [Fact]
    public async Task Handle_WhenSkuCodeNotProvided_ShouldGenerateCode()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, Name: "Auto SKU"),
            TestContext.Current.CancellationToken);

        result.SkuCode.Should().StartWith("SKU-");
        result.SkuCode.Length.Should().BeGreaterThan(4);
    }

    [Fact]
    public async Task Handle_WhenDuplicateSkuCodeSameTenantDifferentCase_ShouldThrowDuplicate()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        var existingSku = product.AddSku(TenantA, "ABC", "Existing SKU", null, null, 0m);
        db.Skus.Add(existingSku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, "abc", "Test SKU", null, null, 10m),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "DUPLICATE_SKU");
    }

    [Fact]
    public async Task Handle_WhenSameSkuCodeDifferentTenant_ShouldCreateSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var productA = Product.Create(TenantA, "PROD-001", "Product A");
        var productB = Product.Create(TenantB, "PROD-002", "Product B");
        db.Set<Product>().AddRange(productA, productB);
        var otherSku = productB.AddSku(TenantB, "ABC", "Other tenant SKU", null, null, 0m);
        db.Skus.Add(otherSku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, productA.Id, "abc", "Test SKU"),
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
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var deletedSku = product.AddSku(TenantA, "ABC", "Deleted SKU", null, null, 0m);
        deletedSku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, "abc", "Test SKU"),
            TestContext.Current.CancellationToken);

        result.SkuCode.Should().Be("abc");
        db.Skus.Count(x => x.TenantId == TenantA && !x.IsDeleted).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenProductBelongsToOtherTenant_ShouldThrowProductNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantB, "PROD-001", "Other Tenant Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, "SKU-001", "Test SKU"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenProductIsSoftDeleted_ShouldThrowProductNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Deleted Product");
        product.MarkDeleted();
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateCreateHandler(db).Handle(
            new CreateSkuCommand(TenantA, product.Id, "SKU-001", "Test SKU"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "PRODUCT_NOT_FOUND");
    }

    [Fact]
    public void Validator_WhenProductIdEmpty_ShouldFail()
    {
        var result = new CreateSkuCommandValidator().Validate(
            new CreateSkuCommand(TenantA, Guid.Empty, "SKU-001"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "ProductId");
    }

    [Fact]
    public void Validator_WhenSkuCodeWhitespace_ShouldFail()
    {
        var result = new CreateSkuCommandValidator().Validate(
            new CreateSkuCommand(TenantA, Guid.NewGuid(), "   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "SkuCode");
    }

    [Fact]
    public void Validator_WhenPriceNegative_ShouldFail()
    {
        var result = new CreateSkuCommandValidator().Validate(
            new CreateSkuCommand(TenantA, Guid.NewGuid(), "SKU-001", Price: -1m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Price");
    }

    private static CreateSkuCommandHandler CreateCreateHandler(WmsDbContext db)
    {
        return new CreateSkuCommandHandler(CreateUnitOfWork(db));
    }
}
