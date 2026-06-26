using Microsoft.EntityFrameworkCore;
using WMS.Application.Reports.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace WMS.Application.Reports.Services;

public class ReportService(IUnitOfWork uow) : IReportService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.AddDays(-6).Date;

        var totalItems = await _uow.Repository<InventoryItem>()
            .CountAsync(x => !x.IsDeleted);

        var inboundThisMonth = await _uow.Repository<InboundOrder>()
            .CountAsync(x => x.CreatedAt >= monthStart && x.Status == InboundStatus.Completed);

        var outboundThisMonth = await _uow.Repository<OutboundOrder>()
            .CountAsync(x => x.CreatedAt >= monthStart && x.Status == OutboundStatus.Shipped);

        var lowStockAlerts = await (from item in _uow.Repository<InventoryItem>().Query()
                                    join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                                    where item.Quantity <= sku.MinQuantity && item.Quantity > 0 && !item.IsDeleted
                                    select item.Id)
                                   .CountAsync(ct);

        var totalValue = await _uow.Repository<InventoryItem>().Query()
            .Where(x => !x.IsDeleted)
            .SumAsync(x => x.UnitPrice * x.Quantity, ct);

        // Weekly data (last 7 days)
        var weeklyInbound = new List<int>();
        var weeklyOutbound = new List<int>();

        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var nextDay = day.AddDays(1);

            var inCount = await _uow.Repository<InboundOrder>().Query()
                .CountAsync(x => x.CreatedAt >= day && x.CreatedAt < nextDay, ct);
            weeklyInbound.Add(inCount);

            var outCount = await _uow.Repository<OutboundOrder>().Query()
                .CountAsync(x => x.CreatedAt >= day && x.CreatedAt < nextDay, ct);
            weeklyOutbound.Add(outCount);
        }

        return new DashboardDto(
            totalItems, inboundThisMonth, outboundThisMonth,
            lowStockAlerts, totalValue, weeklyInbound, weeklyOutbound);
    }

    public async Task<List<AlertDto>> GetLowStockAlertsAsync(CancellationToken ct)
    {
        var items = await (from item in _uow.Repository<InventoryItem>().Query()
                            where !item.IsDeleted
                            join sku in _uow.Repository<Sku>().Query() on item.SkuId equals sku.Id
                            where item.Quantity <= sku.MinQuantity && item.Quantity > 0
                            join loc in _uow.Repository<LocationEntity>().Query() on item.LocationId equals loc.Id
                            orderby item.Quantity
                            select new { item, sku, loc })
                           .ToListAsync(ct);

        return [.. items.Select(x => new AlertDto(
            x.item.Id, 
            x.sku.SkuCode ?? "", 
            x.sku.Name ?? "",
            x.item.Quantity, 
            x.sku.MinQuantity, 
            x.item.Status,
            x.loc.Name ?? ""))];
    }
}