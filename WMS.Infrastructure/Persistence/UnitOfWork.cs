using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using WMS.Domain.Common;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Repositories;

namespace WMS.Infrastructure.Persistence;

public class UnitOfWork(WmsDbContext db, ILogger<UnitOfWork> log) : IUnitOfWork
{
    private readonly WmsDbContext _db = db;
    private readonly ILogger<UnitOfWork> _log = log;
    private readonly ConcurrentDictionary<string, object> _repos = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    // ────────────────────────────────────────────────
    // Repository Access
    // ────────────────────────────────────────────────
    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var key = typeof(T).Name;
        return (IRepository<T>)_repos.GetOrAdd(
            key, _ => new Repository<T>(_db));
    }

    // ────────────────────────────────────────────────
    // SaveChanges — flush ChangeTracker to DB
    // ────────────────────────────────────────────────

    public async Task<int> SaveChangesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var count = await _db.SaveChangesAsync(ct);

            _log.LogDebug(
                "SaveChanges: {Count} entries saved. Tx={HasTx}",
                count, HasActiveTransaction);

            return count;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _log.LogError(ex, "Concurrency conflict during SaveChanges");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Database update error during SaveChanges");
            throw;
        }
    }

    // ────────────────────────────────────────────────
    // Explicit Transaction Management
    // ────────────────────────────────────────────────

    public bool HasActiveTransaction => _transaction != null;

    public async Task<ITransaction> BeginTransactionAsync(
        CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            _log.LogWarning(
                "BeginTransaction called but transaction already active (Id={Id}). " +
                "Returning existing transaction.",
                _transaction.TransactionId);
            return new EfTransaction(_transaction, _log);
        }

        _transaction = await _db.Database
            .BeginTransactionAsync(ct);

        _log.LogInformation(
            "Transaction started (Id={Id})",
            _transaction.TransactionId);

        return new EfTransaction(_transaction, _log);
    }

    public async Task CommitAsync(
        CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            _log.LogDebug("CommitAsync called but no active transaction");
            return;
        }

        try
        {
            // Flush any remaining changes before commit
            await _db.SaveChangesAsync(ct);

            await _transaction.CommitAsync(ct);

            _log.LogInformation(
                "Transaction committed (Id={Id})",
                _transaction.TransactionId);
        }
        catch
        {
            _log.LogError(
                "Commit failed, rolling back (Id={Id})",
                _transaction.TransactionId);
            await _transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            await DisposeTransaction();
        }
    }

    public async Task RollbackAsync(
        CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            _log.LogDebug("RollbackAsync called but no active transaction");
            return;
        }

        try
        {
            await _transaction.RollbackAsync(ct);

            _log.LogWarning(
                "Transaction rolled back (Id={Id})",
                _transaction.TransactionId);
        }
        finally
        {
            // Clear tracked entities to avoid stale state
            DetachAllEntities();
            await DisposeTransaction();
        }
    }

    // ────────────────────────────────────────────────
    // Savepoints
    // ────────────────────────────────────────────────

    public async Task CreateSavepointAsync(string name,
        CancellationToken ct = default)
    {
        EnsureTransactionActive();
        await _transaction!.CreateSavepointAsync(name, ct);

        _log.LogDebug("Savepoint '{Name}' created", name);
    }

    public async Task RollbackToSavepointAsync(string name,
        CancellationToken ct = default)
    {
        EnsureTransactionActive();
        await _transaction!.RollbackToSavepointAsync(name, ct);

        _log.LogDebug("Rolled back to savepoint '{Name}'", name);
    }

    public async Task ReleaseSavepointAsync(string name,
        CancellationToken ct = default)
    {
        EnsureTransactionActive();
        await _transaction!.ReleaseSavepointAsync(name, ct);

        _log.LogDebug("Savepoint '{Name}' released", name);
    }

    // ────────────────────────────────────────────────
    // Disposal
    // ────────────────────────────────────────────────

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Auto-rollback if transaction is still open
        if (_transaction != null)
        {
            _log.LogWarning(
                "UnitOfWork disposed with active transaction — auto-rolling back (Id={Id})",
                _transaction.TransactionId);
            try
            {
                await _transaction.RollbackAsync();
            }
            catch
            {
                // Swallow — we're in Dispose
            }
            await DisposeTransaction();
        }

        await _db.DisposeAsync();
    }

    // ────────────────────────────────────────────────
    // Private Helpers
    // ────────────────────────────────────────────────

    private async Task DisposeTransaction()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private void EnsureTransactionActive()
    {
        if (_transaction == null)
            throw new InvalidOperationException(
                "No active transaction. Call BeginTransactionAsync() first.");
    }

    /// <summary>
    /// Clear ChangeTracker sau rollback để tránh stale data.
    /// </summary>
    private void DetachAllEntities()
    {
        var entries = _db.ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
        _log.LogDebug(
            "Detached {Count} tracked entities after rollback",
            entries.Count);
    }
}