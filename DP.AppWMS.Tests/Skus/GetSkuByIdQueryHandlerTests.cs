using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Queries.GetSkuById;
using WMS.Domain.Entities.Product;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class GetSkuByIdQueryHandlerTests : BaseSkuHandlerTest
{
    #region GetSkuByIdQueryHandler

    [Fact]
    public async Task GetById_WhenSkuExists_ReturnsProductInfo()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Electronics");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.ProductId.Should().Be(product.Id);
        result.ProductCode.Should().Be("PROD-001");
        result.ProductName.Should().Be("Electronics");
        result.SkuCode.Should().Be("SKU-001");
        result.Name.Should().Be("Phone");
        result.ReferencePrice.Should().Be(10m);
    }

    [Fact]
    public async Task GetById_WhenSkuDoesNotExist_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, Guid.NewGuid()), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task GetById_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantB, "PROD-001", "Other Tenant Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantB, "SKU-001", "Phone", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task GetById_WhenSkuIsDeleted_ThrowsNotFound()
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

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task GetById_WhenSkuExists_MapsScalarFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", "Electronic", "Desc", 12.5m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Id.Should().Be(sku.Id);
        result.TenantId.Should().Be(TenantA);
        result.ProductId.Should().Be(product.Id);
        result.SkuCode.Should().Be("SKU-001");
        result.Name.Should().Be("Phone");
        result.GoodsNature.Should().Be("Electronic");
        result.Description.Should().Be("Desc");
        result.ReferencePrice.Should().Be(12.5m);
    }

    [Fact]
    public async Task GetById_WhenNullableFieldsAreNull_PreservesNulls()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, null);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.GoodsNature.Should().BeNull();
        result.Description.Should().BeNull();
        result.ReferencePrice.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenSimilarSkuExistsInOtherTenant_ReturnsRequestedTenantSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var productA = Product.Create(TenantA, "PROD-001", "Product A");
        var productB = Product.Create(TenantB, "PROD-002", "Product B");
        db.Set<Product>().AddRange(productA, productB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requested = productA.AddSku(TenantA, "SKU-001", "Tenant A", null, null, 0m);
        var other = productB.AddSku(TenantB, "SKU-001", "Tenant B", null, null, 0m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, requested.Id), TestContext.Current.CancellationToken);

        result.TenantId.Should().Be(TenantA);
        result.Name.Should().Be("Tenant A");
    }

    [Fact]
    public async Task GetById_WhenCancellationTokenCanceled_ThrowsCancellationException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, Guid.NewGuid()), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Helper Methods

    private static GetSkuByIdQueryHandler CreateGetHandler(WmsDbContext db)
    {
        return new GetSkuByIdQueryHandler(CreateUnitOfWork(db));
    }

    #endregion
}
