using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Reports.DTOs;
using WMS.Application.Reports.Services;

namespace DP.AppWMS.ApiService.Controllers;

public class ReportsController(IReportService svc) : BaseController
{
    private readonly IReportService _svc = svc;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Dashboard(CancellationToken ct)
    {
        var result = await _svc.GetDashboardAsync(ct);
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<List<AlertDto>>>> Alerts(CancellationToken ct)
    {
        var result = await _svc.GetLowStockAlertsAsync(ct);
        return Ok(ApiResponse<List<AlertDto>>.Ok(result));
    }
}
