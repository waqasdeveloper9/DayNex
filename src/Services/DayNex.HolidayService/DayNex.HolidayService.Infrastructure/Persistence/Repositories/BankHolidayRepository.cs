using DayNex.Domain.Common.Interface;
using DayNex.HolidayService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DayNex.Infrastructure.Persistence
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly HolidayDbContext _context;

        public EfRepository(HolidayDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(Guid id) =>
            await _context.Set<T>().FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _context.Set<T>().ToListAsync();

        public async Task<IEnumerable<T>> GetAsync(ISpecification<T> spec)
        {
            var query = _context.Set<T>().AsQueryable();

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Apply specification logic (OrderBy, Paging, Includes) if implemented on ISpecification
            return await query.ToListAsync();
        }

        public async Task AddAsync(T entity) =>
            await _context.Set<T>().AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities) =>
            await _context.Set<T>().AddRangeAsync(entities);

        public async Task UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await _context.SaveChangesAsync(cancellationToken);
    }
}