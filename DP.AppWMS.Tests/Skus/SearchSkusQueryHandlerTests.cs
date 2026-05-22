using FluentAssertions;
using Moq;
using WMS.Application.Product.Skus.Queries.SearchSkus;
using WMS.Domain.Entities;
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

        db.Skus.AddRange(
            CreateSku(TenantA, "A-001", "Tenant A first", updatedAt: BaseTime.AddMinutes(1)),
            CreateSku(TenantA, "A-002", "Tenant A second", updatedAt: BaseTime.AddMinutes(2)),
            CreateSku(TenantA, "A-003", "Deleted", updatedAt: BaseTime.AddMinutes(3), deleteAt: BaseTime.AddMinutes(4)),
            CreateSku(TenantB, "B-001", "Tenant B", updatedAt: BaseTime.AddMinutes(5)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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

        db.Skus.AddRange(
            CreateSku(TenantA, "IPHONE-15", "Apple phone"),
            CreateSku(TenantA, "SAMSUNG-S24", "Android phone"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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

        db.Skus.AddRange(
            CreateSku(TenantA, "SKU-001", "Samsung Galaxy"),
            CreateSku(TenantA, "SKU-002", "Apple iPhone"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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

        db.Skus.AddRange(
            CreateSku(TenantA, "SKU-001", "Scanner", description: "Wireless barcode scanner"),
            CreateSku(TenantA, "SKU-002", "No description", description: null),
            CreateSku(TenantA, "SKU-003", "Printer", description: "Thermal printer"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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

        db.Skus.AddRange(
            CreateSku(TenantA, "ABC-001", "Tenant A item"),
            CreateSku(TenantB, "ABC-001", "Tenant B item"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        var categoryA = CreateCategory(TenantA, "Electronics");
        var categoryB = CreateCategory(TenantA, "Tools");

        db.Categories.AddRange(categoryA, categoryB);
        db.Skus.AddRange(
            CreateSku(TenantA, "ELEC-001", "Phone", categoryA.Id),
            CreateSku(TenantA, "ELEC-002", "Tablet", categoryA.Id),
            CreateSku(TenantA, "TOOL-001", "Hammer", categoryB.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        var categoryA = CreateCategory(TenantA, "Phones");
        var categoryB = CreateCategory(TenantA, "Accessories");

        db.Categories.AddRange(categoryA, categoryB);
        db.Skus.AddRange(
            CreateSku(TenantA, "PHONE-001", "Office phone", categoryA.Id),
            CreateSku(TenantA, "PHONE-002", "Office phone", categoryB.Id),
            CreateSku(TenantA, "LAPTOP-001", "Laptop", categoryA.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.Handle(new SearchSkusQuery(TenantA, Search: "phone", CategoryId: categoryA.Id), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].SkuCode.Should().Be("PHONE-001");
        result.Items[0].CategoryId.Should().Be(categoryA.Id);
    }

    [Fact]
    public async Task Handle_WhenPagingProvided_ReturnsRequestedPageAndTotalCount()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var skus = Enumerable.Range(1, 25)
            .Select(i => CreateSku(TenantA, $"SKU-{i:000}", $"Sku {i}", updatedAt: BaseTime.AddMinutes(i)))
            .ToArray();

        db.Skus.AddRange(skus);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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

        db.Skus.AddRange(
            CreateSku(TenantA, "SKU-001", "First", updatedAt: BaseTime.AddMinutes(1)),
            CreateSku(TenantA, "SKU-002", "Second", updatedAt: BaseTime.AddMinutes(2)),
            CreateSku(TenantA, "SKU-003", "Third", updatedAt: BaseTime.AddMinutes(3)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        var skus = Enumerable.Range(1, 101)
            .Select(i => CreateSku(TenantA, $"SKU-{i:000}", $"Sku {i}", updatedAt: BaseTime.AddMinutes(i)))
            .ToArray();

        db.Skus.AddRange(skus);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        var repository = new Mock<IRepository<SkuEntity>>();
        repository.Setup(x => x.Query()).Returns(db.Skus);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<SkuEntity>()).Returns(repository.Object);

        return new SearchSkusQueryHandler(uow.Object);
    }

    #endregion 
}
