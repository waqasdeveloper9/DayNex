using Microsoft.AspNetCore.Mvc;
using DayNex.HolidayService.Application.Common.Interfaces;

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