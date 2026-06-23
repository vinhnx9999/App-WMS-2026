using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Address,
    string? Phone,
    string? Type) : IRequest<UpdateCustomerResponse>;

public sealed class UpdateCustomerCommandHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<UpdateCustomerResponse> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await _uow.Repository<Customer>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer == null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Customer not found.");
        }

        customer.Update(
            request.Name,
            request.Address,
            request.Phone,
            request.Type);

        await _uow.SaveChangesAsync(ct);

        return new UpdateCustomerResponse(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Address,
            customer.Phone,
            customer.Type,
            customer.CreatedAt,
            customer.UpdatedAt);
    }
}
