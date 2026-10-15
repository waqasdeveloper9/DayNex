using DayNex.Domain.Common.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DayNex.UserInfoService.Infrastructure;
public sealed class UserInfoRepository<T>(UserInfoDbContext db) : IGenericRepository<T> where T : class, IEntity
{
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await db.Set<T>().FindAsync([id], cancellationToken);
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => await db.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = db.Set<T>().AsNoTracking();
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);
        foreach (var include in spec.Includes)
            query = query.Include(include);
        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);
        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => db.Set<T>().AnyAsync(predicate, cancellationToken);
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await db.Set<T>().AddAsync(entity, cancellationToken);
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => db.Set<T>().AddRangeAsync(entities, cancellationToken);
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        db.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await GetByIdAsync(id, cancellationToken);
        if (row is not null)
            db.Remove(row);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
