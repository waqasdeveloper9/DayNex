using DayNex.IdentityService.Domain.Entities;
using DayNex.Shared.Contracts;

namespace DayNex.IdentityService.Application.Interfaces;

public interface ISubscriptionRepository : IGenericRepository<Subscription>
{
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
