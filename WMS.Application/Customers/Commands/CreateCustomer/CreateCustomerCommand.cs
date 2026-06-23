using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Customers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    Guid TenantId,
    string? Code,
    string Name,
    string? Address,
    string? Phone,
    string? Type) : IRequest<CreateCustomerResponse>;

public sealed class CreateCustomerCommandHandler(IUnitOfWork uow, ISequenceCodeGenerator sequenceCodeGenerator)
    : IRequestHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;

    public async Task<CreateCustomerResponse> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var customerCode = string.IsNullOrWhiteSpace(request.Code)
                            ? await _sequenceCodeGenerator.NextAsync(request.TenantId, CodeSequenceTypes.Customer, ct)
                            : request.Code.Trim();

        var existing = await _uow.Repository<Customer>().Query()
            .AnyAsync(x => x.TenantId == request.TenantId && x.Code == customerCode, ct);

        if (existing)
        {
            throw new AppException(400, "CUSTOMER_CODE_ALREADY_EXISTS", "Customer code is already in use.");
        }

        var customer = Customer.Create(
            request.TenantId,
            customerCode,
            request.Name,
            request.Address,
            request.Phone,
            request.Type);

        await _uow.Repository<Customer>().AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateCustomerResponse(
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
