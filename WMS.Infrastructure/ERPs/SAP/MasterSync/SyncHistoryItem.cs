namespace WMS.Infrastructure.ERPs.SAP.MasterSync;

public class SyncHistoryItem
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Status { get; set; }
    public string Details { get; set; }
}