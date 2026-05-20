namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

public class SapGrResult
{
    public bool Success { get; set; }
    public string? MaterialDocument { get; set; }
    public string? MaterialDocumentYear { get; set; }
    public List<string> Messages { get; set; } = [];
}
