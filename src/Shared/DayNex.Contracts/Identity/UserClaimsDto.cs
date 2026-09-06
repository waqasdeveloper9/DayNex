namespace DayNex.Contracts.Identity;

/// <summary>
/// Returned by GET /api/identity/internal/claims/{externalId}. Deliberately
/// smaller than UserProfileDto — every authenticated request on every service
/// fetches this (cached ~3-5 min), so it stays cheap to serialize.
/// </summary>
public sealed record UserClaimsDto(
    string ExternalId,
    string Role,
    string SubscriptionTier);