using DayNex.IdentityService.Domain.Entities;
using DayNex.Shared.Contracts;

namespace DayNex.IdentityService.Application.Interfaces;

/// <summary>
/// Extends the shared generic repository with User-specific lookups that don't make
/// sense as generic methods (e.g. lookup by Entra external id).
/// </summary>
public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
}
