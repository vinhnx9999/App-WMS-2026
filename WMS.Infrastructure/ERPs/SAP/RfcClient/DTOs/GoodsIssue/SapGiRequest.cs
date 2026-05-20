namespace WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;

public class SapGiRequest
{
    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }
    public string ReferenceDoc { get; set; } = "";
    public List<SapGiItem> Items { get; set; } = [];
}
