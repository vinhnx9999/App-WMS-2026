namespace WMS.Infrastructure.ERPs.SAP.DataConfig;

public class SapSyncConfig
{
    public int MasterDataIntervalMinutes { get; set; } = 60;
    public int StockSyncIntervalMinutes { get; set; } = 15;
    public int PurchaseOrderSyncIntervalMinutes { get; set; } = 10;
    public int DeliverySyncIntervalMinutes { get; set; } = 10;
    public int BatchSize { get; set; } = 100;
}