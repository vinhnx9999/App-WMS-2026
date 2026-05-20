using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.SignalR;

namespace DP.AppWMS.ApiService.Controllers;

public class DashboardController(IDashboardNotifier notifier) : BaseController
{
    private readonly IDashboardNotifier _notifier = notifier;

    [HttpGet("connections")]
    public async Task<ActionResult> GetConnections()
    {
        var count = await _notifier.GetConnectedClientsAsync();
        return Ok(ApiResponse.Ok(new
        {
            connectedClients = count,
            hubPath = "/hubs/dashboard",
        }));
    }
}
