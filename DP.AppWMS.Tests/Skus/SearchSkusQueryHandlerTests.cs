using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Skus.Queries.SearchSkus;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class SearchSkusQueryHandlerTests : BaseSkuHandlerTest
{

    #region Testcases

    [Fact]
    public async Task Handle_WhenNoOptionalFilters_ReturnsTenantNonDeletedSkus()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var active1 = await AddTestSku(db, TenantA, "A-001", "Tenant A first");
        var active2 = await AddTestSku(db, TenantA, "A-002", "Tenant A second");
        var deleted = await AddTestSku(db, TenantA, "A-003", "Deleted");
        deleted.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await AddTestSku(db, TenantB, "B-001", "Tenant B");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.TenantId == TenantA);
        result.Items.Select(x => x.SkuCode).Should().NotContain("A-003");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesSkuCode_ReturnsMatchingSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "IPHONE-15", "Apple phone");
        await AddTestSku(db, TenantA, "SAMSUNG-S24", "Android phone");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "iphone"), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].SkuCode.Should().Be("IPHONE-15");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesName_ReturnsMatchingSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "SKU-001", "Samsung Galaxy");
        await AddTestSku(db, TenantA, "SKU-002", "Apple iPhone");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "galaxy"), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Samsung Galaxy");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesDescription_ReturnsMatchingSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "SKU-001", "Scanner", description: "Wireless barcode scanner");
        await AddTestSku(db, TenantA, "SKU-002", "No description", description: null);
        await AddTestSku(db, TenantA, "SKU-003", "Printer", description: "Thermal printer");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "barcode"), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Description.Should().Be("Wireless barcode scanner");
    }

    [Fact]
    public async Task Handle_WhenOtherTenantHasMatchingSku_ExcludesOtherTenantSku()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "ABC-001", "Tenant A item");
        await AddTestSku(db, TenantB, "ABC-001", "Tenant B item");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "abc"), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Should().OnlyContain(x => x.TenantId == TenantA);
        result.Items.Should().NotContain(x => x.TenantId == TenantB);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdProvided_ReturnsOnlyCategorySkus()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var categoryA = Category.Create(TenantA, "Electronics");
        var categoryB = Category.Create(TenantA, "Tools");
        db.Categories.AddRange(categoryA, categoryB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "ELEC-001", "Phone", categoryId: categoryA.Id);
        await AddTestSku(db, TenantA, "ELEC-002", "Tablet", categoryId: categoryA.Id);
        await AddTestSku(db, TenantA, "TOOL-001", "Hammer", categoryId: categoryB.Id);

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, CategoryId: categoryA.Id), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.CategoryId == categoryA.Id);
    }

    [Fact]
    public async Task Handle_WhenSearchAndCategoryProvided_ReturnsOnlySkusMatchingBoth()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var categoryA = Category.Create(TenantA, "Phones");
        var categoryB = Category.Create(TenantA, "Accessories");
        db.Categories.AddRange(categoryA, categoryB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "PHONE-001", "Office phone", categoryId: categoryA.Id);
        await AddTestSku(db, TenantA, "PHONE-002", "Office phone", categoryId: categoryB.Id);
        await AddTestSku(db, TenantA, "LAPTOP-001", "Laptop", categoryId: categoryA.Id);

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "phone", CategoryId: categoryA.Id), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].SkuCode.Should().Be("PHONE-001");
        result.Items[0].CategoryId.Should().Be(categoryA.Id);
    }

    [Fact]
    public async Task Handle_WhenProductIsDeleted_ReturnsSkuWithNullProductAndCategoryFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId, TestContext.Current.CancellationToken);
        product.Delete();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateHandler(db).Handle(new SearchSkusQuery(TenantA), TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(sku.Id);
        result.Items[0].ProductId.Should().BeNull();
        result.Items[0].ProductCode.Should().BeNull();
        result.Items[0].ProductName.Should().BeNull();
        result.Items[0].CategoryId.Should().BeNull();
        result.Items[0].CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenProductRowIsMissing_ReturnsSkuWithNullProductAndCategoryFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", ct: TestContext.Current.CancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId, TestContext.Current.CancellationToken);
        db.Products.Remove(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateHandler(db).Handle(new SearchSkusQuery(TenantA), TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(sku.Id);
        result.Items[0].ProductId.Should().BeNull();
        result.Items[0].ProductCode.Should().BeNull();
        result.Items[0].ProductName.Should().BeNull();
        result.Items[0].CategoryId.Should().BeNull();
        result.Items[0].CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCategoryFilterProvided_ExcludesSkuWithoutMatchingActiveProduct()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var category = Category.Create(TenantA, "Electronics");
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sku = await AddTestSku(db, TenantA, "SKU-001", "Phone", categoryId: category.Id, ct: TestContext.Current.CancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId, TestContext.Current.CancellationToken);
        product.Delete();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateHandler(db).Handle(new SearchSkusQuery(TenantA, CategoryId: category.Id), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPagingProvided_ReturnsRequestedPageAndTotalCount()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        for (int i = 1; i <= 25; i++)
        {
            await AddTestSku(db, TenantA, $"SKU-{i:000}", $"Sku {i}");
        }

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Page: 2, Limit: 10), TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.Items.Select(x => x.SkuCode).Should().Equal(
            "SKU-015", "SKU-014", "SKU-013", "SKU-012", "SKU-011",
            "SKU-010", "SKU-009", "SKU-008", "SKU-007", "SKU-006");
    }

    [Fact]
    public async Task Handle_WhenPageLessThanOne_NormalizesPageToOne()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantA, "SKU-001", "First");
        await AddTestSku(db, TenantA, "SKU-002", "Second");
        await AddTestSku(db, TenantA, "SKU-003", "Third");

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Page: 0, Limit: 10), TestContext.Current.CancellationToken);

        result.PageNumber.Should().Be(1);
        result.Items.Select(x => x.SkuCode).Should().Equal("SKU-003", "SKU-002", "SKU-001");
    }

    [Fact]
    public async Task Handle_WhenLimitGreaterThanOneHundred_ClampsPageSizeToOneHundred()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        for (int i = 1; i <= 101; i++)
        {
            await AddTestSku(db, TenantA, $"SKU-{i:000}", $"Sku {i}");
        }

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Page: 1, Limit: 500), TestContext.Current.CancellationToken);

        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(100);
        result.TotalCount.Should().Be(101);
    }

    #endregion

    #region Helper Methods

    private static SearchSkusQueryHandler CreateHandler(WmsDbContext db)
    {
        var skuRepo = new Mock<IRepository<Sku>>();
        skuRepo.Setup(x => x.Query()).Returns(db.Skus);

        var productRepo = new Mock<IRepository<Product>>();
        productRepo.Setup(x => x.Query()).Returns(db.Products);

        var categoryRepo = new Mock<IRepository<Category>>();
        categoryRepo.Setup(x => x.Query()).Returns(db.Categories);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Sku>()).Returns(skuRepo.Object);
        uow.Setup(x => x.Repository<Product>()).Returns(productRepo.Object);
        uow.Setup(x => x.Repository<Category>()).Returns(categoryRepo.Object);

        return new SearchSkusQueryHandler(uow.Object);
    }

    #endregion
}
