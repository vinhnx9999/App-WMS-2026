using Microsoft.EntityFrameworkCore;
using WMS.Application.Reports.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

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

        var lowStockAlerts = await _uow.Repository<InventoryItem>()
            .CountAsync(x => !x.IsDeleted && x.Status != ItemStatus.InStock);

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
        var items = await _uow.Repository<InventoryItem>().Query()
            .Include(x => x.Location)
            .Where(x => !x.IsDeleted && x.Status != ItemStatus.InStock)
            .OrderBy(x => x.Quantity)
            .ToListAsync(ct);

        return [.. items.Select(x => new AlertDto(
            x.Id, x.Sku?.SkuCode ?? "", x.Name,
            x.Quantity, x.MinQuantity, x.Status,
            x.Location?.Name ?? ""))];
    }
}