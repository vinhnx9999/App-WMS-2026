using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

// Interceptor
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            var auditEntries = new List<AuditLog>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog) continue; // tránh log chính nó

                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                {
                    var audit = new AuditLog
                    {
                        TableName = entry.Metadata?.GetTableName() ?? string.Empty,
                        Action = entry.State.ToString(),
                        KeyValues = string.Join(",", entry.Properties
                            .Where(p => p.Metadata.IsPrimaryKey())
                            .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")),
                        OldValues = entry.State is EntityState.Modified or EntityState.Deleted
                            ? string.Join(",", entry.Properties.Select(p => $"{p.Metadata.Name}={p.OriginalValue}"))
                            : "",
                        NewValues = entry.State is not EntityState.Added and not EntityState.Modified
                            ? ""
                            : string.Join(",", entry.Properties.Select(p => $"{p.Metadata.Name}={p.CurrentValue}")),
                        Timestamp = DateTime.UtcNow
                    };
                    auditEntries.Add(audit);
                }
            }

            if (auditEntries.Count != 0)
            {
                context.Set<AuditLog>().AddRange(auditEntries);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}