using DayNex.HolidayService.Domain.Entities;

namespace DayNex.HolidayService.Application.Common.Interfaces;

public interface IHolidaySyncStore
{
    Task<int> SynchronizeAsync(IReadOnlyCollection<BankHoliday> holidays, CancellationToken cancellationToken);
}
