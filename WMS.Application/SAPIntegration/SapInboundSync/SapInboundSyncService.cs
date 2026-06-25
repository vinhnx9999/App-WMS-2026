using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common.Models;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.SAP.DataClient;
using WMS.Infrastructure.ERPs.SAP.DataConfig;
using WMS.Infrastructure.ERPs.SAP.RfcClient;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

namespace WMS.Application.SAPIntegration.SapInboundSync;

public class SapInboundSyncService(
    ISapODataClient odata,
    ISapRfcClient rfc,
    IUnitOfWork uow,
    IOptions<SapConfig> config,
    ILogger<SapInboundSyncService> log) : ISapInboundSyncService
{
    private readonly ISapODataClient _odata = odata;
    private readonly ISapRfcClient _rfc = rfc;
    private readonly IUnitOfWork _uow = uow;
    private readonly SapMappingConfig _mapping = config.Value.Mapping;
    private readonly ILogger<SapInboundSyncService> _log = log;

    /// <summary>Pull POs từ SAP → tạo InboundOrder trong WMS</summary>
    public async Task<int> SyncPurchaseOrdersAsync(CancellationToken ct)
    {
        _log.LogInformation("Starting SAP PO sync...");

        var filter = $"PurchasingProcessingStatus eq '05'"; // Released
        var doc = await _odata.GetAsync(
            "API_PURCHASEORDER_PROCESS_SRV/A_PurchaseOrder",
            filter, 100, 0, ct);

        if (doc == null) return 0;

        var results = doc.RootElement
            .GetProperty("d")
            .GetProperty("results");

        int synced = 0;
        var orderRepo = _uow.Repository<InboundOrder>();
        var supplierRepo = _uow.Repository<Supplier>();
        var itemRepo = _uow.Repository<InventoryItem>();

        foreach (var po in results.EnumerateArray())
        {
            var poNumber = po.GetProperty("PurchaseOrder").GetString()!;

            // Skip if already synced
            if (await orderRepo.ExistsAsync(o => o.OrderNumber == poNumber))
                continue;

            // Find supplier
            var vendorId = po.GetProperty("Supplier").GetString()!;
            var suppliers = await supplierRepo.FindAsync(
                s => s.Name.Contains(vendorId), ct);
            var supplier = suppliers.FirstOrDefault();

            if (supplier == null)
            {
                _log.LogWarning("Supplier {VendorId} not found in WMS", vendorId);
                continue;
            }

            // Create order
            var order = new InboundOrder
            {
                OrderNumber = poNumber,
                SupplierId = supplier.Id,
                Status = InboundStatus.Pending,
            };

            if (po.TryGetProperty("TotalNetAmount", out var total))
                order.TotalValue = total.GetDecimal();

            await orderRepo.AddAsync(order, ct);
            synced++;

            _log.LogInformation(
                "Synced SAP PO {PONumber} → WMS InboundOrder", poNumber);
        }

        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("SAP PO sync completed: {Count} orders", synced);

        return synced;
    }

    /// <summary>Post Goods Receipt lên SAP sau khi WMS nhận hàng</summary>
    public async Task<SapGrResult> PostGoodsReceiptAsync(
        Guid inboundOrderId, CancellationToken ct)
    {
        var order = await _uow.Repository<InboundOrder>().Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == inboundOrderId, ct)
            ?? throw new AppException(404, "NOT_FOUND", "Đơn nhập không tồn tại");

        if (order.Status != InboundStatus.Completed)
            throw new AppException(400, "NOT_READY",
                "Đơn nhập chưa hoàn tất — chưa thể post GR");

        // Build SAP GR request
        var grRequest = new SapGrRequest
        {
            PostingDate = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            ReferenceDoc = order.OrderNumber,
            HeaderText = $"WMS GR - {order.OrderNumber}",
        };

        var itemRepo = _uow.Repository<InventoryItem>();
        var skuRepo = _uow.Repository<Sku>();
        foreach (var item in order.Items)
        {
            var inv = await itemRepo.GetByIdAsync(item.InventoryItemId, ct);
            var skuName = "";
            if (inv != null)
            {
                var sku = await skuRepo.GetByIdAsync(inv.SkuId, ct);
                skuName = sku?.Name ?? "";
            }
            grRequest.Items.Add(new SapGrItem
            {
                Material = skuName,
                Plant = _mapping.Plant,
                StorageLocation = _mapping.StorageLocation,
                Quantity = item.ReceivedQuantity,
                MoveType = _mapping.MovementTypeGR,
                MovementIndicator = "B", // Goods receipt
                PurchaseOrder = order.OrderNumber,
                PoItem = "00010",
            });
        }

        // Post to SAP
        var result = await _rfc.PostGoodsReceipt(grRequest, ct);

        _log.LogInformation(
            "SAP GR posted for {OrderNumber}: Success={Success}, MatDoc={MatDoc}",
            order.OrderNumber, result.Success, result.MaterialDocument);

        return result;
    }
}
