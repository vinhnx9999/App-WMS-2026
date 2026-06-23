using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Customers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid TenantId, Guid Id) : IRequest<GetCustomerByIdResponse>;

public sealed class GetCustomerByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCustomerByIdQuery, GetCustomerByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetCustomerByIdResponse> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customer = await _uow.Repository<Customer>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer == null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Customer not found.");
        }

        return new GetCustomerByIdResponse(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Address,
            customer.Phone,
            customer.Type,
            customer.IsDeleted,
            customer.CreatedAt,
            customer.UpdatedAt);
    }
}
