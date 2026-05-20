using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WMS.Domain.Interfaces;

namespace WMS.Infrastructure.Repositories;

public class EfTransaction(IDbContextTransaction tx, ILogger log) : ITransaction
{
    private readonly IDbContextTransaction _tx = tx;
    private readonly ILogger _log = log;
    private bool _committed;
    private bool _disposed;

    public Guid TransactionId { get; } = tx.TransactionId;

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction already committed.");

        await _tx.CommitAsync(ct);
        _committed = true;

        _log.LogInformation("Transaction {Id} committed", TransactionId);
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_committed)
            throw new InvalidOperationException(
                "Cannot rollback a committed transaction.");

        await _tx.RollbackAsync(ct);

        _log.LogWarning(
            "Transaction {Id} rolled back", TransactionId);
    }

    public async Task CreateSavepointAsync(string name,
        CancellationToken ct = default)
    {
        await _tx.CreateSavepointAsync(name, ct);
    }

    public async Task RollbackToSavepointAsync(string name,
        CancellationToken ct = default)
    {
        await _tx.RollbackToSavepointAsync(name, ct);
    }

    public async Task ReleaseSavepointAsync(string name,
        CancellationToken ct = default)
    {
        await _tx.ReleaseSavepointAsync(name, ct);
    }

    // ── Auto-rollback on Dispose if not committed ──

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_committed)
        {
            _log.LogWarning(
                "Transaction {Id} disposed without commit — auto-rollback",
                TransactionId);
            try
            {
                await _tx.RollbackAsync();
            }
            catch
            {
                // Swallow during Dispose
            }
        }

        await _tx.DisposeAsync();
    }
}
