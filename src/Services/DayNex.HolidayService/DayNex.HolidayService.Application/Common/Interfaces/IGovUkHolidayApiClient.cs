using DayNex.HolidayService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Application.Common.Interfaces
{
    public interface IGovUkHolidayApiClient
    {
        Task<GovUkApiResponseDto?> FetchBankHolidaysAsync(CancellationToken cancellationToken = default);
    }
}
