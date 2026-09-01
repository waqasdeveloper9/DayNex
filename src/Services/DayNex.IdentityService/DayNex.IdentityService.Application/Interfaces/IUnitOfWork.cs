namespace DayNex.IdentityService.Application.Interfaces;

/// <summary>Coordinates a single SaveChanges call across multiple repositories per request.</summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ISubscriptionRepository Subscriptions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
