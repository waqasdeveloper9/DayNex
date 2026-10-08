using DayNex.Contracts.Identity;

namespace DayNex.IdentityService.Application.Interface;

public interface IUserProfileService
{
    Task<UserProfileDto> GetOrProvisionAsync(string externalId, string? email, string? displayName,
        CancellationToken cancellationToken = default);
    Task<UserPermissionsDto?> GetPermissionsAsync(string externalId, CancellationToken cancellationToken = default);
}
