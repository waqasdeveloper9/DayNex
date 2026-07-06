using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Application.Common.Interfaces
{
    public interface IHolidayRepository
    {
        public Task<List<BankHolidayDto>> GetByRegionAsync(Region region, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
       string title,
       DateOnly date,
       Region region,
       CancellationToken cancellationToken = default);
    }
}
