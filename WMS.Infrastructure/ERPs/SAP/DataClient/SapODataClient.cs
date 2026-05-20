using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WMS.Infrastructure.ERPs.SAP.DataConfig;

namespace WMS.Infrastructure.ERPs.SAP.DataClient;

public class SapODataClient : ISapODataClient
{
    private readonly HttpClient _http;
    private readonly SapODataConfig _config;
    private readonly ILogger<SapODataClient> _log;
    private string? _csrfToken;
    private DateTime _csrfTokenExpiry;

    public SapODataClient(
        IHttpClientFactory httpFactory,
        IOptions<SapConfig> config,
        ILogger<SapODataClient> log)
    {
        _http = httpFactory.CreateClient("SapOData");
        _config = config.Value.OData;
        _log = log;

        // Setup auth
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{_config.Username}:{_config.Password}"));
        _http.BaseAddress = new Uri(_config.BaseUrl);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<JsonDocument?> GetAsync(
        string entitySet, string? filter,
        int top, int skip, CancellationToken ct)
    {
        var url = $"/sap/opu/odata/sap/{entitySet}?$top={top}&$skip={skip}&$format=json";
        if (!string.IsNullOrEmpty(filter))
            url += $"&$filter={Uri.EscapeDataString(filter)}";

        return await ExecuteWithRetry(async () =>
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream);
        }, ct);
    }

    public async Task<JsonDocument?> GetByKeyAsync(
        string entitySet, string key, CancellationToken ct)
    {
        var url = $"/sap/opu/odata/sap/{entitySet}('{key}')?$format=json";

        return await ExecuteWithRetry(async () =>
        {
            var response = await _http.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream);
        }, ct);
    }

    public async Task<bool> PostAsync(
        string entitySet, object payload, CancellationToken ct)
    {
        await EnsureCsrfToken(ct);

        var url = $"/sap/opu/odata/sap/{entitySet}";
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("x-csrf-token", _csrfToken);

        var response = await _http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    private async Task EnsureCsrfToken(CancellationToken ct)
    {
        if (_csrfToken != null && DateTime.UtcNow < _csrfTokenExpiry)
            return;

        var request = new HttpRequestMessage(
            HttpMethod.Head, "/sap/opu/odata/sap/");
        request.Headers.Add("x-csrf-token", "Fetch");

        var response = await _http.SendAsync(request, ct);
        if (response.Headers.TryGetValues(
            "x-csrf-token", out var tokens))
        {
            _csrfToken = tokens.FirstOrDefault();
            _csrfTokenExpiry = DateTime.UtcNow.AddMinutes(25);
        }
    }

    private async Task<T?> ExecuteWithRetry<T>(
        Func<Task<T?>> action, CancellationToken ct)
    {
        for (int i = 0; i <= _config.RetryCount; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (i < _config.RetryCount)
            {
                _log.LogWarning(ex,
                    "SAP OData call failed (attempt {Attempt}/{Max})",
                    i + 1, _config.RetryCount + 1);
                await Task.Delay(
                    _config.RetryDelayMs * (i + 1), ct);
            }
        }
        return default;
    }
}