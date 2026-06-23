using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.Commands.RestoreCustomer;
using WMS.Domain.Common;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class RestoreCustomerCommandHandlerTests : BaseCustomerHandlerTest
{
    private RestoreCustomerCommandHandler CreateHandler(WmsDbContext db)
    {
        return new RestoreCustomerCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithDeletedCustomer_RestoresSuccessfully()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var customer = Customer.Create(TenantA, "CUST01", "Customer Name", "Address 1", "123", "B2B");
        customer.Delete("admin@mail.com");
        await db.Set<Customer>().AddAsync(customer, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new RestoreCustomerCommand(TenantA, customer.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Under normal query, it should now be found
        var queried = await db.Set<Customer>().FirstOrDefaultAsync(x => x.Id == customer.Id, TestContext.Current.CancellationToken);
        queried.Should().NotBeNull();
        queried!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCustomerNotDeleted_ThrowsDomainException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var customer = Customer.Create(TenantA, "CUST01", "Customer Name", "Address 1", "123", "B2B");
        await db.Set<Customer>().AddAsync(customer, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new RestoreCustomerCommand(TenantA, customer.Id);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<DomainException>().Where(x => x.Code == "CUSTOMER_NOT_DELETED");
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new RestoreCustomerCommand(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "CUSTOMER_NOT_FOUND");
    }
}
