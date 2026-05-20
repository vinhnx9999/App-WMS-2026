using System.Text.Json;

namespace WMS.Infrastructure.ERPs.SAP.DataClient;

public interface ISapODataClient
{
    Task<JsonDocument?> GetAsync(string entitySet,
        string? filter = null, int top = 100, int skip = 0,
        CancellationToken ct = default);
    Task<JsonDocument?> GetByKeyAsync(string entitySet,
        string key, CancellationToken ct = default);
    Task<bool> PostAsync(string entitySet,
        object payload, CancellationToken ct = default);
}
