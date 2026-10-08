using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.Shared.Http.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DayNex.IdentityService.WebApi.Controllers;
[ApiController]
[Route("api/identity/me/preferences")]
[Authorize(Policy = AuthenticationDependencies.ApiAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerPreferencesController(ICustomerPreferencesService preferences) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var identity = ExternalIdentity.FromPrincipal(User);
        return identity is null ? Unauthorized() : Ok(await preferences.GetAsync(identity.Key, cancellationToken));
    }
    [HttpPut]
    public async Task<IActionResult> Save(CustomerPreferencesDto input, CancellationToken cancellationToken)
    {
        var identity = ExternalIdentity.FromPrincipal(User);
        if (identity is null) return Unauthorized();
        try { return Ok(await preferences.SaveAsync(identity.Key, input, cancellationToken)); }
        catch (CustomerProfileRequiredException exception) { return Problem(statusCode: 409, title: "Profile setup required", detail: exception.Message); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: "Invalid personal details", detail: exception.Message); }
    }
}
