using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Commands.DeleteCustomer;

public sealed record DeleteCustomerCommand(Guid TenantId, Guid Id) : IRequest;

public sealed class DeleteCustomerCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCustomerCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await _uow.Repository<Customer>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (customer == null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Customer not found.");
        }

        customer.Delete();
        await _uow.SaveChangesAsync(ct);
    }
}
