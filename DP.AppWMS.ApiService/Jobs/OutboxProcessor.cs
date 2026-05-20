using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Jobs;

public class OutboxProcessor(IServiceProvider sp, ILogger<OutboxProcessor> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var pending = await uow.Repository<OutboxMessage>()
                    .FindAsync(m => m.Status == OutboxStatus.Pending, ct);

                foreach (var msg in pending.Take(10))
                {
                    log.LogInformation("Processing outbox: {Type} {Id}", msg.MessageType, msg.Id);
                    // Dispatch to correct handler based on MessageType
                    msg.Status = OutboxStatus.Completed;
                    msg.ProcessedAt = DateTime.UtcNow;
                }
                if (pending.Count > 0) await uow.SaveChangesAsync(ct);
            }
            catch (Exception ex) { log.LogError(ex, "Outbox error"); }

            await Task.Delay(5000, ct);
        }
    }
}
