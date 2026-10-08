using DayNex.HolidayService.Domain.Enums;
namespace DayNex.HolidayService.Application.Common.Interfaces;
public sealed record UpcomingHolidayItem(string Title, DateOnly Date, string? Notes, bool Bunting);
public sealed record UpcomingHolidaySummary(IReadOnlyList<UpcomingHolidayItem> Holidays);
public interface IUpcomingHolidayQuery
{
    Task<UpcomingHolidaySummary> GetAsync(Region region, int limit, CancellationToken cancellationToken);
}
