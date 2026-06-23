using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Customers.Queries.GetCustomerById;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class GetCustomerByIdQueryHandlerTests : BaseCustomerHandlerTest
{
    private GetCustomerByIdQueryHandler CreateHandler(WmsDbContext db)
    {
        return new GetCustomerByIdQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCustomer()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var customer = Customer.Create(TenantA, "CUST01", "Customer One", "Address 1", "12345", "B2B");
        await db.Set<Customer>().AddAsync(customer, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCustomerByIdQuery(TenantA, customer.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Id.Should().Be(customer.Id);
        result.Code.Should().Be("CUST01");
        result.Name.Should().Be("Customer One");
        result.Address.Should().Be("Address 1");
        result.Phone.Should().Be("12345");
        result.Type.Should().Be("B2B");
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ThrowsAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetCustomerByIdQuery(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.Code == "CUSTOMER_NOT_FOUND");
    }
}
