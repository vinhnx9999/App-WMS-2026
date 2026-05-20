using System.Text.Json;

namespace WMS.Infrastructure.ERPs.Odoo.DataClient;

public interface IOdooClient
{
    Task<int> AuthenticateAsync(CancellationToken ct = default);
    Task<JsonElement> CallAsync(string model, string method,
        object?[]? args = null, Dictionary<string, object>? kwargs = null,
        CancellationToken ct = default);
    Task<List<Dictionary<string, object?>>> SearchReadAsync(
        string model, List<object> domain,
        string[] fields, int limit = 100, int offset = 0,
        string? order = null, CancellationToken ct = default);
    Task<int> CreateAsync(string model,
        Dictionary<string, object> values, CancellationToken ct = default);
    Task<bool> WriteAsync(string model, int[] ids,
        Dictionary<string, object> values, CancellationToken ct = default);
    Task<JsonElement> ExecuteMethodAsync(string model, int[] ids,
        string method, CancellationToken ct = default);
}
