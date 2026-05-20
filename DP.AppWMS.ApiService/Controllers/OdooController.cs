using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.OdooIntegration.OdooInboundSync;
using WMS.Application.OdooIntegration.OdooMasterSync;
using WMS.Application.OdooIntegration.OdooOutboundSync;

namespace DP.AppWMS.ApiService.Controllers;

[Authorize(Roles = "admin")]
public class OdooController(
    IOdooInboundSyncService inbound,
    IOdooOutboundSyncService outbound,
    IOdooMasterSyncService master) : BaseController
{
    private readonly IOdooInboundSyncService _inbound = inbound;
    private readonly IOdooOutboundSyncService _outbound = outbound;
    private readonly IOdooMasterSyncService _master = master;

    /// <summary>Pull incoming pickings từ Odoo</summary>
    [HttpPost("sync/inbound")]
    public async Task<ActionResult> SyncInbound(CancellationToken ct)
    {
        var count = await _inbound.SyncInboundPickingsAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} đơn nhập từ Odoo"));
    }

    /// <summary>Confirm GR trong Odoo</summary>
    /// <param name="id"></param>
    [HttpPost("inbound/{id:guid}/confirm")]
    public async Task<ActionResult> ConfirmInbound(
        Guid id, CancellationToken ct)
    {
        await _inbound.ConfirmReceiptAsync(id, ct);
        return Ok(ApiResponse.Ok(null, "Đã xác nhận nhận hàng trên Odoo"));
    }

    /// <summary>
    /// Pull outgoing deliveries từ Odoo
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("sync/outbound")]
    public async Task<ActionResult> SyncOutbound(CancellationToken ct)
    {
        var count = await _outbound.SyncOutboundDeliveriesAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} đơn xuất từ Odoo"));
    }

    /// <summary>
    /// Xác nhận xuất kho trên Odoo
    /// Confirm delivery trong Odoo
    /// Khi xuất kho xong trên WMS, gọi API này để Odoo cập nhật trạng thái đã giao hàng
    /// </summary>    
    [HttpPost("outbound/{id:guid}/confirm")]
    public async Task<ActionResult> ConfirmOutbound(
        Guid id, CancellationToken ct)
    {
        await _outbound.ConfirmDeliveryAsync(id, ct);
        return Ok(ApiResponse.Ok(null, "Đã xác nhận xuất kho trên Odoo"));
    }

    /// <summary>Sync products từ Odoo</summary>
    [HttpPost("sync/products")]
    public async Task<ActionResult> SyncProducts(CancellationToken ct)
    {
        var count = await _master.SyncProductsAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} sản phẩm từ Odoo"));
    }

    /// <summary>Sync partners từ Odoo</summary>
    [HttpPost("sync/partners")]
    public async Task<ActionResult> SyncPartners(CancellationToken ct)
    {
        var count = await _master.SyncPartnersAsync(ct);
        return Ok(ApiResponse.Ok(
            new { synced = count },
            $"Đã đồng bộ {count} đối tác từ Odoo"));
    }

    /// <summary>Check stock từ Odoo</summary>
    [HttpGet("stock/{sku}")]
    public async Task<ActionResult> GetOdooStock(
        string sku, CancellationToken ct)
    {
        var stock = await _master.GetOdooStockAsync(sku, ct);
        return Ok(ApiResponse.Ok(new { sku, odooStock = stock }));
    }
}