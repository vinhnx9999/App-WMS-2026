namespace WMS.Infrastructure.ERPs.SAP.RfcClient;


// Requires: SAP.Connector NuGet package (sapnco)
// Or: SAP.Middleware.Connector for .NET

using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

public interface ISapRfcClient
{
    Task<SapGrResult> PostGoodsReceipt(
        SapGrRequest request, CancellationToken ct = default);
    Task<SapGiResult> PostGoodsIssue(
        SapGiRequest request, CancellationToken ct = default);
    Task<decimal> GetStockAsync(
        string material, string plant, string sloc,
        CancellationToken ct = default);
}

