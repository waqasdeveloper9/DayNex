using DayNex.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DayNex.Shared.Http.Authorization
{
    public class PremiumTierRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Handles Premium-tier gated endpoints. SuperAdmin always succeeds regardless of tier —
    /// this is the "SuperAdmin bypasses everything" rule enforced centrally, once, here.
    /// </summary>
    public class PremiumTierHandler : AuthorizationHandler<PremiumTierRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PremiumTierRequirement requirement)
        {
            if (context.User.IsInRole(UserRole.SuperAdmin.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var tierClaim = context.User.FindFirst(DayNexClaimTypes.SubscriptionTier)?.Value;

            if (Enum.TryParse<SubscriptionTier>(tierClaim, ignoreCase: true, out var tier)
                && tier == SubscriptionTier.Premium)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
