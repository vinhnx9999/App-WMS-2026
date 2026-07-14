using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Queries.GetInboundById;

public sealed class GetInboundByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetInboundByIdQuery, GetInboundByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetInboundByIdResponse> Handle(GetInboundByIdQuery request, CancellationToken ct)
    {
        var order = await _uow.Repository<InboundOrder>().Query()
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (order is null)
        {
            throw new AppException(404, "NOT_FOUND", "PO is not found");
        }

        var skuIds = order.Items.Select(x => x.SkuId).Distinct().ToList();
        var supplierIds = order.Items.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct().ToList();

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

        var itemDtos = order.Items.Select(x =>
        {
            skuDict.TryGetValue(x.SkuId, out var skuInfo);
            string? supplierName = null;
            if (x.SupplierId.HasValue)
            {
                supplierDict.TryGetValue(x.SupplierId.Value, out supplierName);
            }

            return new InboundItemDetailDto(
                x.SkuId,
                skuInfo?.SkuCode,
                skuInfo?.Name,
                x.Quantity,
                x.ReceivedQuantity,
                x.SupplierId,
                supplierName,
                x.ExpiryDate,
                x.SerialNumber,
                x.LotNumber,
                x.Note
            );
        }).ToList();

        return new GetInboundByIdResponse(
            order.Id,
            order.OrderNumber,
            order.ExpectedDate,
            order.ReceivedDate,
            order.Status,
            order.TotalValue,
            order.Notes,
            itemDtos
        );
    }
}
