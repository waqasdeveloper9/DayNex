using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Domain.Enums;

namespace DayNex.HolidayService.Application.Common.Interfaces
{
    public interface IBankHoliday
    {
        //Task<List<BankHolidayDto>> GetByRegionAsync(Region region, CancellationToken cancellationToken = default);
        //Task<BankHolidayDto?> GetNextHolidayAsync(Region region, CancellationToken cancellationToken = default);
        Task<int> SyncHolidaysFromGovApiAsync(CancellationToken cancellationToken = default);
    }
}
