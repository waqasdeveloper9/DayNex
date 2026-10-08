using Microsoft.AspNetCore.Mvc;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace DayNex.HolidayService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankHolidayController : ControllerBase
    {
        private readonly IBankHoliday _bankHolidayService;

        public BankHolidayController(IBankHoliday bankHolidayService)
        {
            _bankHolidayService = bankHolidayService;
        }

        [HttpPost("sync")]
        [Authorize(Policy = DayNexPermissions.HolidaySync)]
        [EnableRateLimiting("holiday-sync")]
        public async Task<IActionResult> SyncHolidays(CancellationToken cancellationToken)
        {
            var result = await _bankHolidayService.SyncHolidaysFromGovApiAsync(cancellationToken);

            return Ok(new
            {
                Message = "Holiday sync completed successfully",
                RecordsProcessed = result
            });
        }



    }
}
