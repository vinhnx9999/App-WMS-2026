using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Application.OdooIntegration.OdooWebhook;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.ERPs.Odoo.OdooWebhook;

namespace WMS.Application.BackgroundJobs;

public class WebhookRetryJob(
    IServiceProvider sp,
    ILogger<WebhookRetryJob> log) : BackgroundService
{
    private readonly IServiceProvider _sp = sp;
    private readonly ILogger<WebhookRetryJob> _log = log;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var uow = scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();
                var webhookService = scope.ServiceProvider
                    .GetRequiredService<IOdooWebhookService>();

                // Tìm các webhook cần retry
                var failed = await uow.Repository<WebhookEvent>()
                    .FindAsync(
                        w => w.Status == "Failed"
                          && w.RetryCount < w.MaxRetries
                          && (w.NextRetryAt == null
                              || w.NextRetryAt <= DateTime.UtcNow),
                        ct);

                foreach (var evt in failed.Take(20))
                {
                    _log.LogInformation(
                        "Retrying webhook {Id} (attempt {Retry}/{Max})",
                        evt.Id, evt.RetryCount + 1, evt.MaxRetries);

                    try
                    {
                        var payload = System.Text.Json.JsonSerializer
                            .Deserialize<OdooWebhookPayload>(evt.Payload);

                        if (payload != null)
                        {
                            evt.Status = "Processing";
                            await uow.SaveChangesAsync(ct);

                            await webhookService.HandleAsync(
                                payload, evt.IpAddress, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        evt.RetryCount++;
                        evt.ErrorMessage = ex.Message;

                        if (evt.RetryCount >= evt.MaxRetries)
                        {
                            evt.Status = "Failed_Permanent";
                            _log.LogError(
                                "Webhook {Id} failed permanently after {Max} retries",
                                evt.Id, evt.MaxRetries);
                        }
                        else
                        {
                            evt.Status = "Failed";
                            // Exponential backoff: 30s, 60s, 120s, 240s, 480s
                            evt.NextRetryAt = DateTime.UtcNow.AddSeconds(
                                Math.Pow(2, evt.RetryCount) * 15);
                        }
                    }
                }

                if (failed.Count > 0)
                    await uow.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Webhook retry job error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
