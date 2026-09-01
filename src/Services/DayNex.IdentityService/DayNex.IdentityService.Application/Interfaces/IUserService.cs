using DayNex.IdentityService.Application.DTOs;
using DayNex.Shared.Contracts;
using DayNex.Shared.Contracts.Enums;

namespace DayNex.IdentityService.Application.Interfaces;

public interface IUserService
{
    /// <summary>Called on first login: creates the local user record if it doesn't exist yet.</summary>
    Task<Result<UserDto>> ProvisionFromExternalLoginAsync(
        ProvisionUserDto dto, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<UserDto>> ChangeRoleAsync(
        Guid targetUserId, UserRole newRole, Guid requestingUserId, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(Guid targetUserId, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(Guid targetUserId, CancellationToken cancellationToken = default);
}
