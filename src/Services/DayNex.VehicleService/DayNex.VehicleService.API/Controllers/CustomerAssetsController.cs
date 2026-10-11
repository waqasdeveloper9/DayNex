using DayNex.Shared.Http.Authentication;
using DayNex.VehicleService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DayNex.VehicleService.API.Controllers;
[ApiController]
[Route("api/assets/me")]
[Authorize(Policy = AuthenticationDependencies.ApiAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerAssetsController(ICustomerAssetService assets) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token) => Ok(await assets.GetAsync(token));
    [HttpPost("vehicles"), EnableRateLimiting("vehicle-lookup")]
    public Task<IActionResult> AddVehicle(AddVehicleRequest input, CancellationToken token) => Execute(async () => Ok(await assets.AddVehicleAsync(input, token)));
    [HttpPost("vehicles/{id:guid}/refresh"), EnableRateLimiting("vehicle-lookup")]
    public Task<IActionResult> Refresh(Guid id, CancellationToken token) => Execute(async () => Ok(await assets.RefreshAsync(id, token)));
    [HttpPost("companies")]
    public Task<IActionResult> AddCompany(AddCompanyRequest input, CancellationToken token) => Execute(async () => Ok(await assets.AddCompanyAsync(input, token)));
    [HttpDelete("vehicles/{id:guid}")]
    public async Task<IActionResult> RemoveVehicle(Guid id, CancellationToken token) => await assets.RemoveVehicleAsync(id, token) ? NoContent() : NotFound();
    [HttpDelete("companies/{id:guid}")]
    public async Task<IActionResult> RemoveCompany(Guid id, CancellationToken token) => await assets.RemoveCompanyAsync(id, token) ? NoContent() : NotFound();
    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: 400, title: "Invalid details", detail: ex.Message);
        }
        catch (AssetNotFoundException)
        {
            return NotFound();
        }
        catch (LookupRateLimitedException)
        {
            Response.Headers.RetryAfter = "300";
            return Problem(statusCode: 429, title: "Wait five minutes between vehicle refreshes.");
        }
    }
}
