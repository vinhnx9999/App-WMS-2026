using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Queries.GetSkuById;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class GetSkuByIdQueryHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task GetById_WhenSkuExists_ReturnsProductInfo()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", referencePrice: 10m, ct: TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.ProductId.Should().Be(sku.ProductId);
        result.ProductCode.Should().Be("PROD-SKU-001");
        result.ProductName.Should().Be("Phone Product");
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
        var sku = await AddTestSku(db, TenantB, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);

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
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        sku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found.");
    }

    [Fact]
    public async Task GetById_WhenProductIsDeleted_ReturnsSkuWithNullProductFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId, TestContext.Current.CancellationToken);
        product.Delete();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Id.Should().Be(sku.Id);
        result.ProductId.Should().BeNull();
        result.ProductCode.Should().BeNull();
        result.ProductName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenProductRowIsMissing_ReturnsSkuWithNullProductFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId, TestContext.Current.CancellationToken);
        db.Products.Remove(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Id.Should().Be(sku.Id);
        result.ProductId.Should().BeNull();
        result.ProductCode.Should().BeNull();
        result.ProductName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenSkuExists_MapsScalarFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", "Desc", 12.5m, ct: TestContext.Current.CancellationToken);
        sku.Update("Phone", "Electronic", "Desc", 12.5m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Id.Should().Be(sku.Id);
        result.TenantId.Should().Be(TenantA);
        result.ProductId.Should().Be(sku.ProductId);
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
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", referencePrice: null, ct: TestContext.Current.CancellationToken);
        sku.Update("Phone", null, null, null);
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
        var requested = await AddTestSku(db, TenantA, "SKU-001", "Tenant A", ct: TestContext.Current.CancellationToken);
        await AddTestSku(db, TenantB, "SKU-001", "Tenant B", ct: TestContext.Current.CancellationToken);

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

    private static GetSkuByIdQueryHandler CreateGetHandler(WmsDbContext db)
    {
        return new GetSkuByIdQueryHandler(CreateUnitOfWork(db));
    }
}
