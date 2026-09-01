using DayNex.IdentityService.Application.DTOs;
using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Domain.Entities;
using DayNex.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace DayNex.IdentityService.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IUnitOfWork unitOfWork, ILogger<SubscriptionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
        return subscription is null
            ? Result<SubscriptionDto>.Failure($"No subscription found for user '{userId}'.", "NOT_FOUND")
            : Result<SubscriptionDto>.Success(MapToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> UpgradeToPremiumAsync(
        Guid userId, DateTime? endDateUtc, CancellationToken cancellationToken = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            // If somehow a user has no subscription row yet, create one directly on Premium.
            subscription = Subscription.CreateTrialPremium(userId);
            await _unitOfWork.Subscriptions.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.UpgradeToPremium(endDateUtc);
            _unitOfWork.Subscriptions.Update(subscription);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} upgraded to Premium", userId);

        return Result<SubscriptionDto>.Success(MapToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> DowngradeToFreeAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result<SubscriptionDto>.Failure($"No subscription found for user '{userId}'.", "NOT_FOUND");

        subscription.Downgrade();
        _unitOfWork.Subscriptions.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} downgraded to Free", userId);
        return Result<SubscriptionDto>.Success(MapToDto(subscription));
    }

    public async Task<Result> CancelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result.Failure($"No subscription found for user '{userId}'.", "NOT_FOUND");

        subscription.Cancel();
        _unitOfWork.Subscriptions.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<bool>> IsPremiumAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
        return Result<bool>.Success(subscription?.IsEffectivelyPremium() ?? false);
    }

    private static SubscriptionDto MapToDto(Subscription s) => new(
        s.Id, s.UserId, s.Tier, s.Status, s.StartDateUtc, s.EndDateUtc, s.IsEffectivelyPremium());
}
