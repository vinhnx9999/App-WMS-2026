using FluentAssertions;
using WMS.Application.Customers.Queries.SearchCustomers;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class SearchCustomersQueryHandlerTests : BaseCustomerHandlerTest
{
    private SearchCustomersQueryHandler CreateHandler(WmsDbContext db)
    {
        return new SearchCustomersQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsActiveCustomersOrderedByName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var c1 = Customer.Create(TenantA, "CUST01", "Beta Customer", "Address 1", "123", "B2B");
        var c2 = Customer.Create(TenantA, "CUST02", "Alpha Customer", "Address 2", "456", "B2C");
        var c3 = Customer.Create(TenantA, "CUST03", "Deleted Customer", "Address 3", "789", "B2B");
        c3.Delete("admin");

        await db.Set<Customer>().AddRangeAsync([c1, c2, c3], TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchCustomersQuery(TenantA, null, 1, 10);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Name.Should().Be("Alpha Customer"); // Ordered by name ascending
        result.Items[1].Name.Should().Be("Beta Customer");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_FiltersByCodeOrName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var c1 = Customer.Create(TenantA, "CUST01", "Apple", "Address 1", "123", "B2B");
        var c2 = Customer.Create(TenantA, "BANANA", "Banana Inc", "Address 2", "456", "B2C");
        await db.Set<Customer>().AddRangeAsync([c1, c2], TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        // Search by name contains
        var query1 = new SearchCustomersQuery(TenantA, "ppl", 1, 10);
        var result1 = await handler.Handle(query1, TestContext.Current.CancellationToken);
        result1.Items.Should().ContainSingle(x => x.Code == "CUST01");

        // Search by code case-insensitive
        var query2 = new SearchCustomersQuery(TenantA, "banana", 1, 10);
        var result2 = await handler.Handle(query2, TestContext.Current.CancellationToken);
        result2.Items.Should().ContainSingle(x => x.Name == "Banana Inc");
    }
}
