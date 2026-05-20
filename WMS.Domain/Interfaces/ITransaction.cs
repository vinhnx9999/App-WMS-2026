namespace WMS.Domain.Interfaces;

public interface ITransaction : IAsyncDisposable, IDisposable
{
    /// <summary>Commit tất cả thay đổi trong transaction.</summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>Rollback toàn bộ transaction.</summary>
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>Tạo savepoint tại điểm hiện tại.</summary>
    Task CreateSavepointAsync(string name,
        CancellationToken ct = default);

    /// <summary>Rollback về savepoint.</summary>
    Task RollbackToSavepointAsync(string name,
        CancellationToken ct = default);

    /// <summary>Giải phóng savepoint.</summary>
    Task ReleaseSavepointAsync(string name,
        CancellationToken ct = default);

    /// <summary>Transaction ID (for logging).</summary>
    Guid TransactionId { get; }
}
