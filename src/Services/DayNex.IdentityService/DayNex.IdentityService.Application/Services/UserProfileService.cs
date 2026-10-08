using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Enums;

namespace DayNex.IdentityService.Application.Services;

public sealed class UserProfileService(IIdentityStore store, TimeProvider timeProvider) : IUserProfileService
{
    public async Task<UserProfileDto> GetOrProvisionAsync(string externalId, string? email, string? displayName,
        CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(externalId, cancellationToken)
            ?? await store.ProvisionAsync(externalId, email, displayName, cancellationToken);
        return new(record.User.Id, record.User.Email, record.User.DisplayName, record.User.Role.ToString(),
            (record.Subscription?.EffectiveTier(timeProvider.GetUtcNow().UtcDateTime) ?? SubscriptionTier.Free).ToString());
    }

    public async Task<UserPermissionsDto?> GetPermissionsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(externalId, cancellationToken);
        if (record is null) return null;
        // A paid subscription never grants administration rights.
        string[] permissions = record.User.Role is UserRole.Admin or UserRole.SuperAdmin
            ? [DayNexPermissions.HolidaySync] : [];
        return new(record.User.Id, permissions);
    }
}
