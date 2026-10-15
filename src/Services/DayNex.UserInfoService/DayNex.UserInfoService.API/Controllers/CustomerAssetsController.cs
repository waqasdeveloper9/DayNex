using DayNex.Shared.Http.Authentication;
using DayNex.UserInfoService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DayNex.UserInfoService.API.Controllers;
[ApiController]
[Route("api/assets/me")]
[Authorize(Policy = AuthenticationDependencies.ApiAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerAssetsController(ICustomerAssetService assets, ICurrentCustomer customer, CompanyProfileService companyProfiles, UserLocationService locations, UserInfoOnboardingService onboarding) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token) => Ok(await assets.GetAsync(token));
    [HttpPost("vehicles"), EnableRateLimiting("vehicle-lookup")]
    public Task<IActionResult> AddVehicle(AddVehicleRequest input, CancellationToken token) => Execute(async () => Ok(await assets.AddVehicleAsync(input, token)));
    [HttpPost("vehicles/{id:guid}/refresh"), EnableRateLimiting("vehicle-lookup")]
    public Task<IActionResult> Refresh(Guid id, CancellationToken token) => Execute(async () => Ok(await assets.RefreshAsync(id, token)));
    [HttpGet("vehicles/{id:guid}/mot")]
    public Task<IActionResult> GetMot(Guid id, CancellationToken token, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Execute(async () => Ok(await assets.GetMotHistoryAsync(id, page, pageSize, token)));
    [HttpPost("companies"), EnableRateLimiting("userinfo-lookup")]
    public Task<IActionResult> AddCompany(AddCompanyRequest input, CancellationToken token) => Execute(async () => Ok(await assets.AddCompanyAsync(input, token)));
    [HttpGet("companies/{id:guid}")]
    public Task<IActionResult> GetCompany(Guid id, CancellationToken token) => Execute(async () =>
        Ok(await companyProfiles.ReadAsync(await customer.GetUserIdAsync(token), id, token)));
    [HttpPost("companies/{id:guid}/refresh"), EnableRateLimiting("userinfo-lookup")]
    public Task<IActionResult> RefreshCompany(Guid id, CancellationToken token) => Execute(async () =>
        Ok(await companyProfiles.RefreshAsync(await customer.GetUserIdAsync(token), id, true, token)));
    [HttpGet("location")]
    public async Task<IActionResult> GetLocation(CancellationToken token) => Ok(await locations.GetAsync(token));
    [HttpPut("location"), EnableRateLimiting("userinfo-lookup")]
    public Task<IActionResult> SaveLocation(UpdateLocationRequest input, CancellationToken token) => Execute(async () => Ok(await locations.SaveAsync(input.Postcode, token)));
    [HttpPost("onboarding"), EnableRateLimiting("userinfo-lookup")]
    public Task<IActionResult> Complete(CompleteUserInfoRequest input, CancellationToken token) =>
        Execute(async () => Ok(await onboarding.CompleteAsync(input, token)));
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
