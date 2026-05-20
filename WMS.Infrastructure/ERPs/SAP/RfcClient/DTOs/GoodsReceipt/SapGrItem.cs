namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

// Requires: SAP.Connector NuGet package (sapnco)
// Or: SAP.Middleware.Connector for .NET

public class SapGrItem
{
    public string Material { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public string MoveType { get; set; } = "101";
    public string? MovementIndicator { get; set; }
    public string? PurchaseOrder { get; set; }
    public string? PoItem { get; set; }
}
