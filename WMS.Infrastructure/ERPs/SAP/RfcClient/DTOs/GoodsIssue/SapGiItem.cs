namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;

public class SapGiItem
{
    public string Material { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public string MoveType { get; set; } = "601";
}
