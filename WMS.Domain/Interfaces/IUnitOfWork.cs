using WMS.Domain.Common;

namespace WMS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // ── Repository Access ──

    /// <summary>
    /// Lấy repository cho entity type T.
    /// Tất cả repo trong cùng UoW instance share cùng DbContext
    /// → cùng transaction khi dùng explicit transaction.
    /// </summary>
    IRepository<T> Repository<T>() where T : BaseEntity;


    // ── Implicit Transaction (SaveChanges wraps in tx) ──

    /// <summary>
    /// Lưu tất cả pending changes vào database.
    /// Nếu đang trong explicit transaction → flush nhưng KHÔNG commit.
    /// Nếu không có explicit transaction → implicit commit.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);


    // ── Explicit Transaction Management ──

    /// <summary>
    /// Bắt đầu database transaction. Mọi SaveChanges sau đó
    /// sẽ nằm trong transaction này cho đến khi Commit/Rollback.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Commit transaction hiện tại. Không throw nếu không có transaction.
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rollback transaction hiện tại. Không throw nếu không có transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>
    /// True nếu đang trong explicit transaction.
    /// </summary>
    bool HasActiveTransaction { get; }


    // ── Savepoints ──

    /// <summary>
    /// Tạo savepoint trong transaction hiện tại.
    /// Cho phép rollback một phần mà không rollback toàn bộ.
    /// </summary>
    Task CreateSavepointAsync(string name,
        CancellationToken ct = default);

    /// <summary>
    /// Rollback về savepoint đã tạo trước đó.
    /// Các thay đổi sau savepoint bị hủy, transaction vẫn tiếp tục.
    /// </summary>
    Task RollbackToSavepointAsync(string name,
        CancellationToken ct = default);

    /// <summary>
    /// Giải phóng savepoint (giải phóng tài nguyên DB server).
    /// </summary>
    Task ReleaseSavepointAsync(string name,
        CancellationToken ct = default);
}