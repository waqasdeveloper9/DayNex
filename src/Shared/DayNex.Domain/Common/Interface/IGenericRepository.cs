using DayNex.Domain.Common.Entity;

namespace DayNex.Domain.Common.Interface
{
    /// <summary>
    /// A strict, leakage-free abstraction over storage engines. 
    /// Avoids IQueryable leaks to protect architecture boundaries.
    /// </summary>
    public interface IGenericRepository<T> : IReadRepository<T>, IWriteRepository<T>
        where T : class, IEntity
    {
    }
}
