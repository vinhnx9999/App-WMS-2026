using Microsoft.Extensions.Diagnostics.HealthChecks;
using WMS.Infrastructure.ERPs.Odoo.DataClient;

namespace WMS.Application.OdooIntegration.HealthCheck;

public class OdooConnectionHealthCheck(IOdooClient client) : IHealthCheck
{
    private readonly IOdooClient _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct)
    {
        try
        {
            var uid = await _client.AuthenticateAsync(ct);
            return uid > 0
                ? HealthCheckResult.Healthy($"Odoo OK (uid={uid})")
                : HealthCheckResult.Unhealthy("Auth returned uid=0");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Odoo unreachable", ex);
        }
    }
}