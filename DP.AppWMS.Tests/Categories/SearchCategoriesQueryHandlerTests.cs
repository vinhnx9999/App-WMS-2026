using FluentAssertions;
using WMS.Application.Categories.Queries.SearchCategories;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Categories;

public sealed class SearchCategoriesQueryHandlerTests : BaseCategoryHandlerTest
{
    private static SearchCategoriesQueryHandler CreateHandler(WmsDbContext db)
    {
        return new SearchCategoriesQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsActivePagedCategories()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed categories
        db.Set<Category>().AddRange(
            Category.Create(TenantA, "Cat A", "Desc A"),
            Category.Create(TenantA, "Cat B", "Desc B"),
            Category.Create(TenantA, "Cat C", "Desc C")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchCategoriesQuery(TenantA, null, 1, 2); // Page 1, Limit 2

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithKeywordMatchingName_ReturnsFilteredMatches()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        db.Set<Category>().AddRange(
            Category.Create(TenantA, "Hardware Tools", "Desc A"),
            Category.Create(TenantA, "Office Supplies", "Desc B"),
            Category.Create(TenantA, "Warehousing", "Desc C")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchCategoriesQuery(TenantA, "ware", 1, 10);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2); // "Hardware Tools" and "Warehousing" both match "ware"
        result.TotalCount.Should().Be(2);
        result.Items.Select(x => x.Name).Should().Contain(new[] { "Hardware Tools", "Warehousing" });
    }

    [Fact]
    public async Task Handle_WithDeletedCategoriesPresent_ExcludesDeletedCategories()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var catA = Category.Create(TenantA, "Cat A");
        var catB = Category.Create(TenantA, "Cat B");
        catB.MarkDeleted("user");

        db.Set<Category>().AddRange(catA, catB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchCategoriesQuery(TenantA, null, 1, 10);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(1);
        result.Items.First().Id.Should().Be(catA.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EnforcesTenantIsolation()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        db.Set<Category>().AddRange(
            Category.Create(TenantA, "Tenant A Cat"),
            Category.Create(TenantB, "Tenant B Cat")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchCategoriesQuery(TenantA, null, 1, 10);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Tenant A Cat");
        result.TotalCount.Should().Be(1);
    }
}
