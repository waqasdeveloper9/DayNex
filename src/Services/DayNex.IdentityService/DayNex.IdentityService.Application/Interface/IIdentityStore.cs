using DayNex.IdentityService.Domain.Entities;

namespace DayNex.IdentityService.Application.Interface;

public sealed record IdentityRecord(User User, Subscription? Subscription);

public interface IIdentityStore
{
    Task<IdentityRecord?> FindAsync(string externalId, CancellationToken cancellationToken);
    Task<IdentityRecord> ProvisionAsync(string externalId, string? email, string? displayName, CancellationToken cancellationToken);
}
