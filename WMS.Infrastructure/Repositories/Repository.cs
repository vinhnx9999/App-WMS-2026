using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WMS.Domain.Common;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Repositories;

public class Repository<T>(WmsDbContext db) : IRepository<T> where T : BaseEntity
{
    protected readonly WmsDbContext _db = db;
    protected readonly DbSet<T> _set = db.Set<T>();

    // ── Query ──
    public IQueryable<T> Query() => _set.AsQueryable();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _set.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct) =>
        await _set.Where(x => !x.IsDeleted)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct) =>
        await _set.Where(x => !x.IsDeleted)
            .Where(predicate).ToListAsync(ct);

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null) =>
        predicate is null
            ? await _set.CountAsync(x => !x.IsDeleted)
            : await _set.Where(x => !x.IsDeleted)
                        .Where(predicate)
                        .CountAsync();

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) =>
        await _set.Where(x => !x.IsDeleted)
                  .Where(predicate)
                  .AnyAsync();

    // ── Command (không gọi SaveChanges — UoW quản lý) ──
    public async Task<T> AddAsync(T entity,
        CancellationToken ct)
    {
        var result = await _set.AddAsync(entity, ct);
        return result.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        await _set.AddRangeAsync(entities, ct);
    }

    public Task UpdateAsync(T entity)
    {
        _set.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        entity.MarkDeleted();  // soft delete
        _set.Update(entity);
        return Task.CompletedTask;
    }

    public Task AttachAsync(T entity)
    {
        _set.Attach(entity);
        return Task.CompletedTask;
    }
}