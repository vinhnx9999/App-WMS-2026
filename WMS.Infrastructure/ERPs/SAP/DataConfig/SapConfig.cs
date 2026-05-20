namespace WMS.Infrastructure.ERPs.SAP.DataConfig;

public class SapConfig
{
    public bool Enabled { get; set; }
    public SapConnectionConfig Connection { get; set; } = new();
    public SapODataConfig OData { get; set; } = new();
    public SapMappingConfig Mapping { get; set; } = new();
    public SapSyncConfig Sync { get; set; } = new();
}
