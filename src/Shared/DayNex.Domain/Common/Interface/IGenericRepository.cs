using DayNex.Domain.Common.Entity;

namespace DayNex.Domain.Common.Interface
{
    /// <summary>
    /// A strict, leakage-free abstraction over storage engines. 
    /// Avoids IQueryable leaks to protect architecture boundaries.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAsync(ISpecification<T> spec);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);  
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);  
    }
}
