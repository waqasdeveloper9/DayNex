using DayNex.IdentityService.Application.DTOs;
using DayNex.Shared.Contracts;

namespace DayNex.IdentityService.Application.Interfaces;

public interface ISubscriptionService
{
    Task<Result<SubscriptionDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<SubscriptionDto>> UpgradeToPremiumAsync(
        Guid userId, DateTime? endDateUtc, CancellationToken cancellationToken = default);

    Task<Result<SubscriptionDto>> DowngradeToFreeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result> CancelAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Used by other microservices' feature gates and by the JWT-claim enrichment step.</summary>
    Task<Result<bool>> IsPremiumAsync(Guid userId, CancellationToken cancellationToken = default);
}
