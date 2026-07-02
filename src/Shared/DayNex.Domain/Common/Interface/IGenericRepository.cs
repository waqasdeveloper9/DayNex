using DayNex.Domain.Common.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.Domain.Common.Interface
{
    /// <summary>
    /// A strict, leakage-free abstraction over storage engines. 
    /// Avoids IQueryable leaks to protect architecture boundaries.
    /// </summary>
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the entity state tracker. (Note: EF Core tracking handles updates in memory, 
        /// but this explicit statement is provided for semantic completeness).
        /// </summary>
        void Update(T entity);

        void Delete(T entity);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
