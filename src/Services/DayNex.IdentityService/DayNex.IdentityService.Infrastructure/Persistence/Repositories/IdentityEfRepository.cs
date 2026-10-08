using System.Linq.Expressions;
using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;

public class IdentityEfRepository<T> : IGenericRepository<T> where T : class, IEntity
{
    protected readonly IdentityDbContext Context;

    public IdentityEfRepository(IdentityDbContext context) => Context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> GetAsync(
        ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator<T>.GetQuery(Context.Set<T>().AsNoTracking(), spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AnyAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AddRangeAsync(entities, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        if (entity is not null)
            Context.Set<T>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Context.SaveChangesAsync(cancellationToken);
}