using System.Security.Claims;
using DayNex.IdentityService.Application.DTOs;
using DayNex.IdentityService.Application.Interfaces;
using DayNex.Shared.Http.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DayNex.IdentityService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // every endpoint below requires a valid JWT unless overridden
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Called right after a successful Entra External ID login (from the frontend or
    /// Gateway) to provision/sync the local user record. Idempotent — safe to call every login.
    /// </summary>
    [HttpPost("provision")]
    [AllowAnonymous] // token has just been issued by Entra; caller is authenticated at the IdP, not yet locally
    public async Task<IActionResult> Provision(
        [FromBody] ProvisionUserDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.ProvisionFromExternalLoginAsync(dto, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { result.Error, result.ErrorCode });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var externalId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(externalId))
            return Unauthorized();

        // NOTE: for a real deployment, resolve external->local id via IUserRepository.GetByExternalIdAsync
        // exposed through the service; simplified here to keep the controller thin.
        return Ok(new { message = "Use GET /api/v1/users/{id} with the id returned from /provision." });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { result.Error, result.ErrorCode });
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Only SuperAdmin can change roles — Admin cannot promote/demote other users.</summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = PolicyNames.RequireSuperAdmin)]
    public async Task<IActionResult> ChangeRole(
        Guid id, [FromBody] ChangeUserRoleDto dto, CancellationToken cancellationToken)
    {
        var requestingUserId = GetCurrentUserGuid();
        var result = await _userService.ChangeRoleAsync(id, dto.NewRole, requestingUserId, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { result.Error, result.ErrorCode });
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeactivateAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { result.Error, result.ErrorCode });
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.ActivateAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { result.Error, result.ErrorCode });
    }

    private Guid GetCurrentUserGuid()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
