using System.Text.Json;

namespace WMS.Infrastructure.ERPs.SAP.DataClient;

public class NoOpSapODataClient : ISapODataClient
{
    public Task<JsonDocument?> GetAsync(string entitySet, string? filter = null, int top = 100, int skip = 0, CancellationToken ct = default)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public Task<JsonDocument?> GetByKeyAsync(string entitySet, string key, CancellationToken ct = default)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public Task<bool> PostAsync(string entitySet, object payload, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
