using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Suppliers.DTOs;
using WMS.Domain.Entities.Master;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Suppliers.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(
    Guid TenantId,
    string? Code,
    string Name,
    string? Contact,
    string? Phone,
    string? Email,
    string? Address) : IRequest<CreateSupplierResponse>;

public sealed class CreateSupplierCommandHandler(IUnitOfWork uow, ISequenceCodeGenerator sequenceCodeGenerator)
    : IRequestHandler<CreateSupplierCommand, CreateSupplierResponse>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _sequenceCodeGenerator = sequenceCodeGenerator;

    public async Task<CreateSupplierResponse> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var supplierCode = string.IsNullOrWhiteSpace(request.Code)
                            ? await _sequenceCodeGenerator.NextAsync(request.TenantId, CodeSequenceTypes.Supplier, ct)
                            : request.Code.Trim();

        var existing = await _uow.Repository<Supplier>().Query()
            .AnyAsync(x => x.TenantId == request.TenantId && x.Code == supplierCode, ct);

        if (existing)
        {
            throw new AppException(400, "SUPPLIER_CODE_ALREADY_EXISTS", "Supplier code is already in use.");
        }

        var supplier = Supplier.Create(
            request.TenantId,
            supplierCode,
            request.Name,
            request.Contact,
            request.Phone,
            request.Email,
            request.Address);

        await _uow.Repository<Supplier>().AddAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateSupplierResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.Contact,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.CreatedAt,
            supplier.UpdatedAt);
    }

}
