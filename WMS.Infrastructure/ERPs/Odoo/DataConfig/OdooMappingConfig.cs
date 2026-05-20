namespace WMS.Infrastructure.ERPs.Odoo.DataConfig;

public class OdooMappingConfig
{
    public int WarehouseId { get; set; } = 1;
    public int StockLocationId { get; set; } = 8;
    public int IncomingPickingTypeId { get; set; } = 1;
    public int OutgoingPickingTypeId { get; set; } = 2;
    public int SupplierLocationId { get; set; } = 4;
    public int CustomerLocationId { get; set; } = 5;
}
