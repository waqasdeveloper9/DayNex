using System.Security.Claims;
using DayNex.Contracts.Identity;

namespace DayNex.Shared.Http.Authorization;

public interface IPermissionSource
{
    Task<UserPermissionsDto?> GetAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
