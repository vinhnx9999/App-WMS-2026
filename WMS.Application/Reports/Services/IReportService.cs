using WMS.Application.Reports.DTOs;

namespace WMS.Application.Reports.Services;

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<List<AlertDto>> GetLowStockAlertsAsync(CancellationToken ct = default);
}
