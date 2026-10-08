using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Domain.Enums;
using DayNex.Shared.Http.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DayNex.HolidayService.API.Controllers;
[ApiController]
[Route("api/holidays/upcoming")]
[Authorize(Policy = AuthenticationDependencies.ApiAccess)]
public sealed class UpcomingHolidaysController(IUpcomingHolidayQuery query) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string region = "EnglandAndWales", [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Region>(region, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed) ||
            region != parsed.ToString() || limit is < 1 or > 10)
            return Problem(statusCode: 400, title: "Choose a supported holiday region and a limit from 1 to 10.");
        return Ok(await query.GetAsync(parsed, limit, cancellationToken));
    }
}
