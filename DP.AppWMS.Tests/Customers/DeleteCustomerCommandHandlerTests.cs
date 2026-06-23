using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.Commands.DeleteCustomer;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class DeleteCustomerCommandHandlerTests : BaseCustomerHandlerTest
{
    private DeleteCustomerCommandHandler CreateHandler(WmsDbContext db)
    {
        return new DeleteCustomerCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidId_SoftDeletesCustomer()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var customer = Customer.Create(TenantA, "CUST01", "Customer Name", "Address 1", "123", "B2B");
        await db.Set<Customer>().AddAsync(customer, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteCustomerCommand(TenantA, customer.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Under normal query (excluding deleted), it should not be found
        var queried = await db.Set<Customer>().FirstOrDefaultAsync(x => x.Id == customer.Id && !x.IsDeleted, TestContext.Current.CancellationToken);
        queried.Should().BeNull();

        // Bypassing filter, it should exist and have IsDeleted = true
        var raw = await db.Set<Customer>().IgnoreQueryFilters().FirstAsync(x => x.Id == customer.Id, TestContext.Current.CancellationToken);
        raw.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteCustomerCommand(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "CUSTOMER_NOT_FOUND");
    }
}
