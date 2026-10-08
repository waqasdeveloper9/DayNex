using DayNex.IdentityService.Application.Interface;
using DayNex.Shared.Http.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DayNex.IdentityService.WebApi.Controllers;

[ApiController]
[Route("api/identity")]
[Authorize(Policy = AuthenticationDependencies.ApiAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IdentityController(IUserProfileService profiles) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var identity = ExternalIdentity.FromPrincipal(User);
        if (identity is null) return Unauthorized();
        return Ok(await profiles.GetOrProvisionAsync(identity.Key, identity.Email, identity.DisplayName, cancellationToken));
    }

    [HttpGet("me/permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var identity = ExternalIdentity.FromPrincipal(User);
        if (identity is null) return Unauthorized();
        var permissions = await profiles.GetPermissionsAsync(identity.Key, cancellationToken);
        return permissions is null ? NotFound() : Ok(permissions);
    }
}
