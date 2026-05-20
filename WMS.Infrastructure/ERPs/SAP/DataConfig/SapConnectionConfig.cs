namespace WMS.Infrastructure.ERPs.SAP.DataConfig;

public class SapConnectionConfig
{
    public string AppServerHost { get; set; } = "";
    public string SystemNumber { get; set; } = "00";
    public string SystemId { get; set; } = "";
    public string Client { get; set; } = "100";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Language { get; set; } = "EN";
    public int PoolSize { get; set; } = 10;
    public int PeakConnectionsLimit { get; set; } = 20;
}
