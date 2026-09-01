using DayNex.IdentityService.Application.DTOs;
using DayNex.IdentityService.Application.Interfaces;
using DayNex.Shared.Http.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DayNex.IdentityService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetByUserIdAsync(userId, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { result.Error, result.ErrorCode });
    }

    [HttpGet("user/{userId:guid}/is-premium")]
    public async Task<IActionResult> IsPremium(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.IsPremiumAsync(userId, cancellationToken);
        return Ok(new { userId, isPremium = result.Data });
    }

    /// <summary>
    /// Manual/admin upgrade path for now (no payment gateway yet). Once billing is wired up,
    /// this becomes the endpoint the payment webhook calls internally.
    /// </summary>
    [HttpPost("user/{userId:guid}/upgrade")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> Upgrade(
        Guid userId, [FromBody] UpgradeSubscriptionDto dto, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.UpgradeToPremiumAsync(userId, dto.EndDateUtc, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { result.Error, result.ErrorCode });
    }

    [HttpPost("user/{userId:guid}/downgrade")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> Downgrade(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.DowngradeToFreeAsync(userId, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { result.Error, result.ErrorCode });
    }

    [HttpPost("user/{userId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.CancelAsync(userId, cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { result.Error, result.ErrorCode });
    }

    /// <summary>Example of a Premium-gated endpoint using the shared policy + SuperAdmin bypass.</summary>
    [HttpGet("premium-feature-sample")]
    [Authorize(Policy = PolicyNames.RequirePremium)]
    public IActionResult PremiumFeatureSample()
        => Ok(new { message = "You have access — you're either Premium or SuperAdmin." });
}
