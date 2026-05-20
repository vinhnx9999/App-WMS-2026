namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;

public class SapGiResult
{
    public bool Success { get; set; }
    public string? MaterialDocument { get; set; }
    public List<string> Messages { get; set; } = [];
}