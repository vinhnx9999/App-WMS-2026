namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

// ── DTOs ──

public class SapGrRequest
{
    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }
    public string ReferenceDoc { get; set; } = "";
    public string? HeaderText { get; set; }
    public List<SapGrItem> Items { get; set; } = [];
}
