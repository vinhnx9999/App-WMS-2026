namespace WMS.Infrastructure.ERPs.Odoo.DataConfig;

public class OdooSyncConfig
{
    public int MasterDataIntervalMinutes { get; set; } = 60;
    public int StockSyncIntervalMinutes { get; set; } = 15;
    public int PickingSyncIntervalMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 200;
}