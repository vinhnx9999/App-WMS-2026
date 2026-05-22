using FluentAssertions;
using FluentValidation;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Queries.GetSkuById;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class GetSkuByIdQueryHandlerTests : BaseSkuHandlerTest
{
    #region GetSkuByIdQueryHandler

    [Fact]
    public async Task GetById_WhenSkuHasCategory_ReturnsCategoryName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = CreateCategory(TenantA, "Electronics");
        var sku = CreateSku(TenantA, "SKU-001", "Phone", category.Id);
        db.Categories.Add(category);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.CategoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetById_WhenSkuHasNoCategory_ReturnsNullCategoryName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenSkuDoesNotExist_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, Guid.NewGuid()), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task GetById_WhenSkuBelongsToOtherTenant_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantB, "SKU-001", "Phone");
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task GetById_WhenSkuIsDeleted_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone");
        sku.MarkDeleted();
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "NOT_FOUND" && x.Message == "SKU not found");
    }

    [Fact]
    public async Task GetById_WhenSkuExists_MapsScalarFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var categoryId = Guid.NewGuid();
        var sku = CreateSku(TenantA, "SKU-001", "Phone", categoryId, "Desc", 12.5m, BaseTime.AddDays(-1), BaseTime.AddDays(1));
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Id.Should().Be(sku.Id);
        result.TenantId.Should().Be(TenantA);
        result.CategoryId.Should().Be(categoryId);
        result.SkuCode.Should().Be("SKU-001");
        result.Name.Should().Be("Phone");
        result.Description.Should().Be("Desc");
        result.Price.Should().Be(12.5m);
        result.CreatedAt.Should().Be(BaseTime.AddDays(-1));
        result.UpdatedAt.Should().Be(BaseTime.AddDays(1));
    }

    [Fact]
    public async Task GetById_WhenNullableFieldsAreNull_PreservesNulls()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = CreateSku(TenantA, "SKU-001", "Phone", description: null, price: null);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.Description.Should().BeNull();
        result.Price.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenCategoryIdSetButNavigationMissing_ReturnsCategoryIdAndNullCategoryName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var categoryId = Guid.NewGuid();
        var sku = CreateSku(TenantA, "SKU-001", "Phone", categoryId);
        db.Skus.Add(sku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateGetHandler(db).Handle(new GetSkuByIdQuery(TenantA, sku.Id), TestContext.Current.CancellationToken);

        result.CategoryId.Should().Be(categoryId);
        result.CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenSimilarSkuExistsInOtherTenant_ReturnsRequestedTenantSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var requested = CreateSku(TenantA, "SKU-001", "Tenant A");
        db.Skus.AddRange(requested, CreateSku(TenantB, "SKU-001", "Tenant B"));
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