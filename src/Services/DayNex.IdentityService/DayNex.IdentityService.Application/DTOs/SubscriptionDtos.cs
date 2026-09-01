using DayNex.Shared.Contracts.Enums;

namespace DayNex.IdentityService.Application.DTOs;

public record SubscriptionDto(
    Guid Id,
    Guid UserId,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime StartDateUtc,
    DateTime? EndDateUtc,
    bool IsEffectivelyPremium);

public class UpgradeSubscriptionDto
{
    public DateTime? EndDateUtc { get; set; }
}
