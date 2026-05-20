using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.SAPIntegration.SapInboundSync;
using WMS.Application.SAPIntegration.SapMasterSync;

namespace DP.AppWMS.ApiService.Controllers;

[Authorize(Roles = "admin")]
public class SapController(
    ISapInboundSyncService inboundSync,
    ISapMasterSyncService masterSync,
    ILogger<SapController> log) : BaseController
{
    private readonly ISapInboundSyncService _inboundSync = inboundSync;
    private readonly ISapMasterSyncService _masterSync = masterSync;
    private readonly ILogger<SapController> _log = log;

    /// <summary>Pull Purchase Orders từ SAP</summary>
    [HttpPost("sync/purchase-orders")]
    public async Task<ActionResult> SyncPurchaseOrders(
        CancellationToken ct)
    {
        var count = await _inboundSync.SyncPurchaseOrdersAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} đơn nhập từ SAP"));
    }

    /// <summary>Post Goods Receipt lên SAP</summary>
    [HttpPost("inbound/{id:guid}/post-gr")]
    public async Task<ActionResult> PostGoodsReceipt(
        Guid id, CancellationToken ct)
    {
        var result = await _inboundSync.PostGoodsReceiptAsync(id, ct);

        if (result.Success)
            return Ok(ApiResponse.Ok(result,
                $"GR posted — Material Doc: {result.MaterialDocument}"));

        return BadRequest(ApiResponse.Ok(result, "SAP GR posting failed"));
    }

    /// <summary>Pull Deliveries từ SAP</summary>
    [HttpPost("sync/deliveries")]
    public async Task<ActionResult> SyncDeliveries(
        CancellationToken ct)
    {
        var count = await _masterSync.SyncDeliveriesAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} đơn xuất từ SAP"));
    }

    /// <summary>Sync Material Master</summary>
    [HttpPost("sync/materials")]
    public async Task<ActionResult> SyncMaterials(
        CancellationToken ct)
    {
        var count = await _masterSync.SyncMaterialsAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} materials từ SAP"));
    }

    /// <summary>Check stock from SAP</summary>
    [HttpGet("stock/{material}")]
    public async Task<ActionResult> GetSapStock(
        string material, CancellationToken ct)
    {
        var stock = await _masterSync.GetSapStockAsync(material, ct);
        return Ok(ApiResponse.Ok(new { material, stock }));
    }

    /// <summary>Sync status / history</summary>
    [HttpGet("sync/history")]
    public async Task<ActionResult> GetSyncHistory(
        CancellationToken ct)
    {
        var logs = await _masterSync.GetSyncHistoryAsync(ct);
        return Ok(ApiResponse.Ok(logs));
    }
}
