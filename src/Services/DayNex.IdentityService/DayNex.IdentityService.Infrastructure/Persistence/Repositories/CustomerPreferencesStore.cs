using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;
public sealed class CustomerPreferencesStore(IdentityDbContext context) : ICustomerPreferencesStore
{
    public Task<CustomerPreferences?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<CustomerPreferences>().AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    public async Task SaveAsync(CustomerPreferences value, CancellationToken cancellationToken)
    {
        if (await UpdateAsync(value, cancellationToken) > 0) return;
        context.Add(value);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Another instance inserted the first preferences row. Last submitted update wins.
            context.Entry(value).State = EntityState.Detached;
            if (await UpdateAsync(value, cancellationToken) == 0) throw;
        }
    }
    private Task<int> UpdateAsync(CustomerPreferences value, CancellationToken cancellationToken) =>
        context.Set<CustomerPreferences>().Where(p => p.UserId == value.UserId).ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.Postcode, value.Postcode)
            .SetProperty(p => p.HolidayRegion, value.HolidayRegion)
            .SetProperty(p => p.UpdatedUtc, value.UpdatedUtc), cancellationToken);
}
