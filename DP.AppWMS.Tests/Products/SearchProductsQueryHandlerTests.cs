using FluentAssertions;
using WMS.Application.Products.Queries.SearchProducts;
using WMS.Domain.Entities.Product;

namespace DP.AppWMS.Tests.Products;

public sealed class SearchProductsQueryHandlerTests : BaseProductHandlerTest
{
    [Fact]
    public async Task Handle_SearchByKeyword_ReturnsMatchingProducts()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        db.Products.Add(Product.Create(TenantA, "PRD-MATCH-1", "MatchName One", "Some Desc"));
        db.Products.Add(Product.Create(TenantA, "PRD-MATCH-2", "Other Name", "MatchDesc Two"));
        db.Products.Add(Product.Create(TenantA, "PRD-NOMATCH", "Other Name", "Other Desc"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchProductsQueryHandler(CreateUnitOfWork(db));

        // Test 1: Search by code keyword
        var result1 = await handler.Handle(new SearchProductsQuery(TenantA, "match-1", null, 1, 10), TestContext.Current.CancellationToken);
        result1.Items.Should().HaveCount(1);
        result1.Items.First().ProductCode.Should().Be("PRD-MATCH-1");

        // Test 2: Search by name keyword
        var result2 = await handler.Handle(new SearchProductsQuery(TenantA, "one", null, 1, 10), TestContext.Current.CancellationToken);
        result2.Items.Should().HaveCount(1);
        result2.Items.First().ProductName.Should().Be("MatchName One");

        // Test 3: Search by description keyword
        var result3 = await handler.Handle(new SearchProductsQuery(TenantA, "desc two", null, 1, 10), TestContext.Current.CancellationToken);
        result3.Items.Should().HaveCount(1);
        result3.Items.First().ProductCode.Should().Be("PRD-MATCH-2");
    }

    [Fact]
    public async Task Handle_FilterByCategory_ReturnsOnlyCategoryProducts()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var cat1 = await AddTestCategory(db, TenantA, "Cat 1", TestContext.Current.CancellationToken);
        var cat2 = await AddTestCategory(db, TenantA, "Cat 2", TestContext.Current.CancellationToken);

        db.Products.Add(Product.Create(TenantA, "PRD-C1", "P1", categoryId: cat1.Id));
        db.Products.Add(Product.Create(TenantA, "PRD-C2", "P2", categoryId: cat2.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchProductsQueryHandler(CreateUnitOfWork(db));

        var result = await handler.Handle(new SearchProductsQuery(TenantA, null, cat1.Id, 1, 10), TestContext.Current.CancellationToken);
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductCode.Should().Be("PRD-C1");
        result.Items.First().CategoryName.Should().Be("Cat 1");
    }

    [Fact]
    public async Task Handle_TenantBoundary_IsolateProducts()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        db.Products.Add(Product.Create(TenantA, "PRD-A", "P A"));
        db.Products.Add(Product.Create(TenantB, "PRD-B", "P B"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchProductsQueryHandler(CreateUnitOfWork(db));

        var result = await handler.Handle(new SearchProductsQuery(TenantA, null, null, 1, 10), TestContext.Current.CancellationToken);
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductCode.Should().Be("PRD-A");
    }

    [Fact]
    public async Task Handle_PagedResult_ReturnsCorrectPagingTotals()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        for (int i = 1; i <= 15; i++)
        {
            db.Products.Add(Product.Create(TenantA, $"PRD-{i:00}", $"Name {i}"));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchProductsQueryHandler(CreateUnitOfWork(db));

        var result = await handler.Handle(new SearchProductsQuery(TenantA, null, null, 2, 5), TestContext.Current.CancellationToken);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }
}
