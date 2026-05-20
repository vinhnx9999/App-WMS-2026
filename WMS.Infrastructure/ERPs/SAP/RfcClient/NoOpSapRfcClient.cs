namespace WMS.Infrastructure.ERPs.SAP.RfcClient;


// Requires: SAP.Connector NuGet package (sapnco)
// Or: SAP.Middleware.Connector for .NET

using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

public class NoOpSapRfcClient : ISapRfcClient
{
    public Task<decimal> GetStockAsync(string material, string plant, string sloc, CancellationToken ct = default)
    {
        return Task.FromResult(0m);
    }

    public Task<SapGiResult> PostGoodsIssue(SapGiRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new SapGiResult
        {
            Success = true,
            MaterialDocument = "TEST_DOC",
            Messages = ["No-op GI successful"]
        });
    }

    public Task<SapGrResult> PostGoodsReceipt(SapGrRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new SapGrResult
        {
            Success = true,
            MaterialDocument = "TEST_DOC",
            Messages = ["No-op GR successful"]
        });
    }
}
