using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Commands.RestoreCustomer;

public sealed record RestoreCustomerCommand(Guid TenantId, Guid Id) : IRequest;

public sealed class RestoreCustomerCommandHandler(IUnitOfWork uow) : IRequestHandler<RestoreCustomerCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(RestoreCustomerCommand request, CancellationToken ct)
    {
        var customer = await _uow.Repository<Customer>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer == null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Customer not found.");
        }

        customer.Restore();
        await _uow.SaveChangesAsync(ct);
    }
}
