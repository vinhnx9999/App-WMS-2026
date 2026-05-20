using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;

namespace WMS.Infrastructure.ERPs.Odoo.DataClient;

public class OdooJsonRpcClient : IOdooClient
{
    private readonly HttpClient _http;
    private readonly OdooConfig _config;
    private readonly ILogger<OdooJsonRpcClient> _log;
    private int _uid;
    private int _requestId;

    public OdooJsonRpcClient(
        IHttpClientFactory httpFactory,
        IOptions<OdooConfig> config,
        ILogger<OdooJsonRpcClient> log)
    {
        _http = httpFactory.CreateClient("Odoo");
        _config = config.Value;
        _log = log;

        _http.BaseAddress = new Uri(_config.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <summary>Authenticate and get uid</summary>
    public async Task<int> AuthenticateAsync(CancellationToken ct)
    {
        var response = await SendRpcAsync(new
        {
            service = "common",
            method = "authenticate",
            args = new object[]
            {
                _config.Database,
                _config.Username,
                _config.Password,
                new { }
            }
        }, ct);

        _uid = response.GetInt32();

        // Capture session cookie if present
        if (_http.DefaultRequestHeaders.Contains("Cookie"))
            _http.DefaultRequestHeaders.Remove("Cookie");

        _log.LogInformation(
            "Odoo authenticated: uid={Uid}, db={Db}",
            _uid, _config.Database);

        return _uid;
    }

    /// <summary>Generic execute_kw call</summary>
    public async Task<JsonElement> CallAsync(
        string model, string method,
        object?[]? args = null,
        Dictionary<string, object>? kwargs = null,
        CancellationToken ct = default)
    {
        await EnsureAuthenticated(ct);

        var allArgs = new List<object>
        {
            _config.Database, _uid, _config.Password,
            model, method
        };

        if (args != null && args.Length > 0)
            allArgs.AddRange(collection: args);

        return await ExecuteWithRetry(async () =>
            await SendRpcAsync(new
            {
                service = "object",
                method = "execute_kw",
                args = allArgs.ToArray()
            }, ct), ct);
    }

    /// <summary>search_read — the most used method</summary>
    public async Task<List<Dictionary<string, object?>>> SearchReadAsync(
        string model, List<object> domain,
        string[] fields, int limit, int offset,
        string? order, CancellationToken ct)
    {
        var kwargs = new Dictionary<string, object>
        {
            ["fields"] = fields,
            ["limit"] = limit,
            ["offset"] = offset,
        };

        if (!string.IsNullOrEmpty(order))
            kwargs["order"] = order;

        var result = await CallAsync(model, "search_read",
            [domain, kwargs], ct: ct);

        var list = new List<Dictionary<string, object?>>();

        foreach (var item in result.EnumerateArray())
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in item.EnumerateObject())
            {
                dict[prop.Name] = ConvertJsonElement(prop.Value);
            }
            list.Add(dict);
        }

        return list;
    }

    /// <summary>create() — returns new record ID</summary>
    public async Task<int> CreateAsync(string model,
        Dictionary<string, object> values, CancellationToken ct)
    {
        var result = await CallAsync(model, "create", [values], ct: ct);

        return result.GetInt32();
    }

    /// <summary>write() — update records</summary>
    public async Task<bool> WriteAsync(string model, int[] ids,
        Dictionary<string, object> values, CancellationToken ct)
    {
        var result = await CallAsync(model, "write", [ids, values], ct: ct);

        return result.GetBoolean();
    }

    /// <summary>Execute record method (e.g. button_validate)</summary>
    public async Task<JsonElement> ExecuteMethodAsync(
        string model, int[] ids, string method,
        CancellationToken ct)
    {
        return await CallAsync(model, method,
            [ids], ct: ct);
    }

    // ── Private helpers ──

    private async Task EnsureAuthenticated(CancellationToken ct)
    {
        if (_uid == 0)
            await AuthenticateAsync(ct);
    }

    private async Task<JsonElement> SendRpcAsync(
        object payload, CancellationToken ct)
    {
        var body = new
        {
            jsonrpc = "2.0",
            method = "call",
            id = Interlocked.Increment(ref _requestId),
            @params = payload
        };

        var response = await _http.PostAsJsonAsync("jsonrpc", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadFromJsonAsync<JsonDocument>(ct);

        var root = json!.RootElement;

        // Check for Odoo error
        if (root.TryGetProperty("error", out var error))
        {
            var msg = error.TryGetProperty("data", out var data)
                ? data.GetProperty("message").GetString()
                : error.GetProperty("message").GetString();

            _log.LogError("Odoo RPC error: {Message}", msg);
            throw new OdooException(msg ?? "Unknown Odoo error");
        }

        return root.GetProperty("result");
    }

    private async Task<T> ExecuteWithRetry<T>(
        Func<Task<T>> action, CancellationToken ct)
    {
        for (int i = 0; i <= _config.RetryCount; i++)
        {
            try
            {
                return await action();
            }
            catch (OdooSessionExpiredException)
            {
                _log.LogWarning("Odoo session expired, re-authenticating...");
                _uid = 0;
                await AuthenticateAsync(ct);
                return await action();
            }
            catch (Exception ex) when (i < _config.RetryCount)
            {
                _log.LogWarning(ex,
                    "Odoo call failed (attempt {Attempt}/{Max})",
                    i + 1, _config.RetryCount + 1);
                await Task.Delay(_config.RetryDelayMs * (i + 1), ct);
            }
        }
        throw new OdooException("Max retries exceeded");
    }

    private static object? ConvertJsonElement(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l)
                ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => el.EnumerateArray()
                .Select(ConvertJsonElement).ToList(),
            _ => el.GetRawText()
        };
}
