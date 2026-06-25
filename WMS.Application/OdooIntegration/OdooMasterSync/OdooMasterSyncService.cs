using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common.Service;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;
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

    /// <summary>Sync products từ Odoo → WMS Sku & Product</summary>
    public async Task<int> SyncProductsAsync(CancellationToken ct)
    {
        _log.LogInformation("Syncing products from Odoo...");

        var domain = new List<object>
        {
            new object[] { "type", "=", "product" },
            new object[] { "active", "=", true },
        };

        int offset = 0, synced = 0;
        var skuRepo = _uow.Repository<Sku>();
        var productRepo = _uow.Repository<Product>();
        var categoryRepo = _uow.Repository<Category>();
        var tenantId = _currentUser.TenantId;

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
                var skuCode = p.TryGetValue("default_code", out var dc)
                    ? dc?.ToString() : "";
                if (string.IsNullOrEmpty(skuCode)) continue;

                var name = p.TryGetValue("name", out var n)
                    ? n?.ToString() ?? "" : "";
                var barcode = p.TryGetValue("barcode", out var b)
                    ? b?.ToString() : null;
                var price = p.TryGetValue("standard_price", out var pr)
                    ? ToDecimal(pr) : 0;

                // Sync category
                Guid? categoryId = null;
                if (p.TryGetValue("categ_id", out var catObj) && catObj is List<object> catList && catList.Count >= 2)
                {
                    var catName = catList[1].ToString() ?? "";
                    if (!string.IsNullOrEmpty(catName))
                    {
                        var existingCat = (await categoryRepo.FindAsync(c => c.Name == catName, ct)).FirstOrDefault();
                        if (existingCat != null)
                        {
                            categoryId = existingCat.Id;
                        }
                        else
                        {
                            var newCat = Category.Create(tenantId, catName);
                            await categoryRepo.AddAsync(newCat, ct);
                            categoryId = newCat.Id;
                        }
                    }
                }

                // Sync Product
                var existingProduct = (await productRepo.FindAsync(x => x.ProductCode == skuCode, ct)).FirstOrDefault();
                if (existingProduct == null)
                {
                    existingProduct = Product.Create(tenantId, skuCode, name, description: "", categoryId: categoryId);
                    await productRepo.AddAsync(existingProduct, ct);
                }
                else
                {
                    existingProduct.Update(name, description: "", categoryId: categoryId);
                }

                // Sync Sku
                var existingSku = (await skuRepo.FindAsync(x => x.SkuCode == skuCode, ct)).FirstOrDefault();
                if (existingSku == null)
                {
                    var newSku = Sku.Create(
                        tenantId: tenantId,
                        productId: existingProduct.Id,
                        skuCode: skuCode,
                        name: name,
                        goodsNature: "Odoo product",
                        description: "",
                        referencePrice: price,
                        barcode: barcode
                    );
                    await skuRepo.AddAsync(newSku, ct);
                }
                else
                {
                    existingSku.Update(
                        name: name,
                        goodsNature: "Odoo product",
                        description: "",
                        referencePrice: price,
                        barcode: barcode
                    );
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
                var code = await _codeGenerator.NextAsync(_currentUser.TenantId, CodeSequenceTypes.Customer, ct);

                await partnerRepo.AddAsync(Customer.Create(
                    tenantId: _currentUser.TenantId,
                    code: code,
                    name: name,
                    phone: phone,
                    type: CodeSequenceTypes.Customer
                ), ct);
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