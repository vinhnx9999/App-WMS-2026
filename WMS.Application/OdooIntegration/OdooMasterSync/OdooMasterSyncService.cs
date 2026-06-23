using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common.Service;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Product;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.DataClient;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;

namespace WMS.Application.OdooIntegration.OdooMasterSync;

public class OdooMasterSyncService(
    IOdooClient odoo, IUnitOfWork uow,
    IOptions<OdooConfig> config,
    ICurrentUser currentUser,
    ISequenceCodeGenerator codeGenerator,
    ILogger<OdooMasterSyncService> log) : IOdooMasterSyncService
{
    private readonly IOdooClient _odoo = odoo;
    private readonly IUnitOfWork _uow = uow;
    private readonly OdooMappingConfig _mapping = config.Value.Mapping;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ISequenceCodeGenerator _codeGenerator = codeGenerator;
    private readonly ILogger<OdooMasterSyncService> _log = log;

    /// <summary>Sync products từ Odoo → WMS InventoryItem</summary>
    public async Task<int> SyncProductsAsync(CancellationToken ct)
    {
        _log.LogInformation("Syncing products from Odoo...");

        var domain = new List<object>
        {
            new object[] { "type", "=", "product" },
            new object[] { "active", "=", true },
        };

        int offset = 0, synced = 0;
        var repo = _uow.Repository<InventoryItem>();

        while (true)
        {
            var products = await _odoo.SearchReadAsync(
                "product.product", domain,
                [
                    "id", "default_code", "name", "barcode",
                    "standard_price", "categ_id",
                    "description_sale", "uom_id"
                ],
                limit: 200, offset: offset, ct: ct);

            if (products.Count == 0) break;

            foreach (var p in products)
            {
                var sku = p.TryGetValue("default_code", out var dc)
                    ? dc?.ToString() : "";
                if (string.IsNullOrEmpty(sku)) continue;

                var existing = (await repo.FindAsync(
                    x => $"{x.Sku.SkuCode}" == sku, ct)).FirstOrDefault();

                var name = p.TryGetValue("name", out var n)
                    ? n?.ToString() ?? "" : "";
                var barcode = p.TryGetValue("barcode", out var b)
                    ? b?.ToString() : null;
                var price = p.TryGetValue("standard_price", out var pr)
                    ? ToDecimal(pr) : 0;

                if (existing != null)
                {
                    // Update
                    existing.Name = name;
                    existing.Barcode = barcode;
                    existing.UnitPrice = price;
                    existing.UpdateStatus();
                }
                else
                {
                    var itemSku = await _uow.Repository<Sku>().FindAsync(
                        s => $"{s.SkuCode}" == sku, ct);
                    var skuId = itemSku?.FirstOrDefault()?.Id ?? Guid.Empty;
                    // Create
                    var item = new InventoryItem
                    {
                        SkuId = skuId,
                        Name = name,
                        Barcode = barcode,
                        UnitPrice = price,
                        Status = ItemStatus.OutOfStock,
                    };
                    await repo.AddAsync(item, ct);
                }

                synced++;
            }

            offset += products.Count;
        }

        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("Product sync done: {Count} items", synced);
        return synced;
    }

    /// <summary>Sync partners (suppliers + customers)</summary>
    public async Task<int> SyncPartnersAsync(CancellationToken ct)
    {
        var domain = new List<object>
        {
            new object[] { "|" },
            new object[] { "supplier_rank", ">", 0 },
            new object[] { "customer_rank", ">", 0 },
        };

        var partners = await _odoo.SearchReadAsync(
            "res.partner", domain,
            new[]
            {
                "id", "name", "phone", "email",
                "street", "city", "supplier_rank", "customer_rank"
            },
            limit: 500, ct: ct);

        int synced = 0;
        var supplierRepo = _uow.Repository<Supplier>();
        var partnerRepo = _uow.Repository<Customer>();

        foreach (var p in partners)
        {
            var name = p.TryGetValue("name", out var n)
                ? n?.ToString() ?? "" : "";
            if (string.IsNullOrEmpty(name)) continue;

            var isSupplier = p.TryGetValue("supplier_rank", out var sr)
                && ToDecimal(sr) > 0;
            var isCustomer = p.TryGetValue("customer_rank", out var cr)
                && ToDecimal(cr) > 0;

            var phone = p.TryGetValue("phone", out var ph)
                ? ph?.ToString() : null;
            var email = p.TryGetValue("email", out var em)
                ? em?.ToString() : null;

            if (isSupplier && !await supplierRepo.ExistsAsync(
                s => s.Name == name))
            {
                var code = await _codeGenerator.NextAsync(_currentUser.TenantId, CodeSequenceTypes.Supplier, ct);

                await supplierRepo.AddAsync(Supplier.Create(
                    tenantId: _currentUser.TenantId,
                    code: code,
                    name: name,
                    phone: phone,
                    email: email
                ), ct);
                synced++;
            }

            if (isCustomer && !await partnerRepo.ExistsAsync(
                pt => pt.Name == name))
            {
                await partnerRepo.AddAsync(new Customer
                {
                    Name = name,
                    Phone = phone,
                    Type = "Customer"
                }, ct);
                synced++;
            }
        }

        await _uow.SaveChangesAsync(ct);
        return synced;
    }

    /// <summary>Get real-time stock from Odoo</summary>
    public async Task<decimal> GetOdooStockAsync(
        string sku, CancellationToken ct)
    {
        var quants = await _odoo.SearchReadAsync(
            "stock.quant",
            [
                new object[] { "product_id.default_code", "=", sku },
                new object[] { "location_id", "=", _mapping.StockLocationId },
            ],
            ["quantity", "reserved_quantity"],
            ct: ct);

        return quants.Sum(q =>
            q.TryGetValue("quantity", out var v) ? ToDecimal(v) : 0);
    }

    private static decimal ToDecimal(object? val) =>
        val switch
        {
            double d => (decimal)d,
            long l => l,
            int i => i,
            string s when decimal.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d) => d,
            _ => 0
        };
}