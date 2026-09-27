using DayNex.Contracts.Identity;
using DayNex.Domain.Common.Entity;

namespace DayNex.IdentityService.Application.Interface
{
    public interface IUserProfileService
    {
        Task<Result<UserProfileDto>> GetOrProvisionAsync(
        string externalId, string email, string displayName, CancellationToken cancellationToken = default);

        Task<Result<UserClaimsDto>> GetClaimsAsync(
            string externalId, CancellationToken cancellationToken = default);
    }
}
