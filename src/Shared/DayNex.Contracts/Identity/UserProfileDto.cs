namespace DayNex.Contracts.Identity;

/// <summary>
/// Returned by GET /api/identity/me. Crosses the network boundary to Angular —
/// shape only, no behavior, per Contracts project convention.
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string SubscriptionTier);