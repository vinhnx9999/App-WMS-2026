using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using WMS.Infrastructure.ERPs.SAP.DataClient;

namespace WMS.Application.SAPIntegration.HealthCheck;

public class SapConnectionHealthCheck : IHealthCheck
{
    private readonly ISapODataClient _client;

    public SapConnectionHealthCheck(ISapODataClient client)
        => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct)
    {
        try
        {
            var result = await _client.GetAsync(
                "API_MATERIAL_SRV/A_MaterialType",
                top: 1, ct: ct);

            return result != null
                ? HealthCheckResult.Healthy("SAP OData reachable")
                : HealthCheckResult.Degraded("SAP returned empty");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SAP unreachable", ex);
        }
    }
}