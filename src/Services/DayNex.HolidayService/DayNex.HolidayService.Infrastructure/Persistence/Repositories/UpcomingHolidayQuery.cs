using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace DayNex.HolidayService.Infrastructure.Persistence.Repositories;
public sealed class UpcomingHolidayQuery(HolidayDbContext context, TimeProvider clock) : IUpcomingHolidayQuery
{
    public async Task<UpcomingHolidaySummary> GetAsync(Region region, int limit, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(region)) throw new ArgumentOutOfRangeException(nameof(region));
        if (limit is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(limit));
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/London")).DateTime);
        var items = await context.BankHoliday.AsNoTracking().Where(h => h.Region == region && h.Date >= today)
            .OrderBy(h => h.Date).ThenBy(h => h.Title).Take(limit)
            .Select(h => new UpcomingHolidayItem(h.Title, h.Date, h.Notes, h.Bunting)).ToListAsync(cancellationToken);
        return new(items);
    }
}
