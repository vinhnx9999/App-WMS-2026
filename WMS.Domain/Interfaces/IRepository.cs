using System.Linq.Expressions;
using WMS.Domain.Common;

namespace WMS.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    // ── Query ──
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    // ── Command ──
    // Add/Update/Delete chỉ track trong ChangeTracker.
    // Phải gọi SaveChangesAsync trên UoW để flush xuống DB.
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);

    /// <summary>
    /// Attach entity mà không mark as modified.
    /// Hữu ích khi muốn update một số field cụ thể.
    /// </summary>
    Task AttachAsync(T entity);
}
