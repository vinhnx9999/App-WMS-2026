using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.Commands.UpdateCustomer;
using WMS.Application.Customers.Validators;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class UpdateCustomerCommandHandlerTests : BaseCustomerHandlerTest
{
    private UpdateCustomerCommandHandler CreateHandler(WmsDbContext db)
    {
        return new UpdateCustomerCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesCustomer()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var customer = Customer.Create(TenantA, "CUST01", "Old Name", "Old Address", "123", "Old Type");
        await db.Set<Customer>().AddAsync(customer, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateCustomerCommand(
            customer.Id,
            TenantA,
            "New Name",
            "New Address",
            "456",
            "New Type");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Id.Should().Be(customer.Id);
        result.Code.Should().Be("CUST01");
        result.Name.Should().Be("New Name");
        result.Address.Should().Be("New Address");
        result.Phone.Should().Be("456");
        result.Type.Should().Be("New Type");

        var updated = await db.Set<Customer>().IgnoreQueryFilters().FirstAsync(x => x.Id == customer.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("New Name");
        updated.Address.Should().Be("New Address");
        updated.Phone.Should().Be("456");
        updated.Type.Should().Be("New Type");
    }

    [Fact]
    public async Task Handle_WithInvalidId_ThrowsAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateCustomerCommand(
            Guid.NewGuid(),
            TenantA,
            "New Name",
            "New Address",
            "456",
            "New Type");

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.Code == "CUSTOMER_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_WhenNameEmpty_ShouldFail(string? emptyVal)
    {
        var validator = new UpdateCustomerCommandValidator();
        var result = validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), TenantA, emptyVal!, null, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_WhenFieldsExceedLength_ShouldFail()
    {
        var validator = new UpdateCustomerCommandValidator();

        var result = validator.Validate(new UpdateCustomerCommand(
            Guid.NewGuid(),
            TenantA,
            new string('B', 256),
            new string('C', 256),
            new string('D', 21),
            new string('E', 51)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
        result.Errors.Should().Contain(x => x.PropertyName == "Address");
        result.Errors.Should().Contain(x => x.PropertyName == "Phone");
        result.Errors.Should().Contain(x => x.PropertyName == "Type");
    }
}
