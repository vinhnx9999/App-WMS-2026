using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Queries.GetInboundReceiptById;

public sealed record GetInboundReceiptByIdQuery(Guid TenantId, Guid Id) : IRequest<GetInboundReceiptByIdResponse>;

public sealed class GetInboundReceiptByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetInboundReceiptByIdQuery, GetInboundReceiptByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetInboundReceiptByIdResponse> Handle(GetInboundReceiptByIdQuery request, CancellationToken ct)
    {
        var receipt = await _uow.Repository<InboundReceipt>().Query()
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (receipt is null)
        {
            throw new AppException(404, "NOT_FOUND", "Inbound Receipt not found");
        }

        string? orderNumber = null;
        if (receipt.InboundOrderId.HasValue)
        {
            var order = await _uow.Repository<InboundOrder>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == receipt.InboundOrderId.Value && x.TenantId == request.TenantId && !x.IsDeleted, ct);
            orderNumber = order?.OrderNumber;
        }

        var warehouse = await _uow.Repository<WMS.Domain.Entities.WarehouseAggregateRoot.Warehouse>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == receipt.WarehouseId && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        var skuIds = receipt.Items.Select(x => x.SkuId).Distinct().ToList();
        var supplierIds = receipt.Items.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct().ToList();

        var skuDict = await _uow.Repository<WMS.Domain.Entities.SkuAggregateRoot.Sku>().Query()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted && skuIds.Contains(x.Id))
            .Select(x => new { x.Id, x.SkuCode, x.Name })
            .ToDictionaryAsync(x => x.Id, x => new { x.SkuCode, x.Name }, ct);

        var supplierDict = supplierIds.Any()
            ? await _uow.Repository<WMS.Domain.Entities.Master.Supplier>().Query()
                .Where(x => x.TenantId == request.TenantId && !x.IsDeleted && supplierIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct)
            : new Dictionary<Guid, string>();

        var itemDtos = receipt.Items.Select(x =>
        {
            skuDict.TryGetValue(x.SkuId, out var skuInfo);
            string? supplierName = null;
            if (x.SupplierId.HasValue)
            {
                supplierDict.TryGetValue(x.SupplierId.Value, out supplierName);
            }

            return new InboundReceiptItemDetailDto(
                x.SkuId,
                skuInfo?.SkuCode,
                skuInfo?.Name,
                x.ExpectedQuantity,
                x.ReceivedQuantity,
                x.Notes,
                x.SupplierId,
                supplierName,
                x.ExpiryDate,
                x.SerialNumber,
                x.LotNumber
            );
        }).ToList();

        return new GetInboundReceiptByIdResponse(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.InboundOrderId,
            orderNumber,
            receipt.WarehouseId,
            warehouse?.Name,
            receipt.Status,
            receipt.CreatedAt,
            itemDtos
        );
    }
}
