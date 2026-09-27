using DayNex.Contracts.Identity;
using DayNex.Domain.Common.Entity;
using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Application.Specifications;
using DayNex.IdentityService.Domain.Entities;

namespace DayNex.IdentityService.Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Subscription> _subscriptionRepository;

    public UserProfileService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Subscription> subscriptionRepository)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<UserProfileDto>> GetOrProvisionAsync(
        string externalId, string email, string displayName, CancellationToken cancellationToken = default)
    {
        var user = (await _userRepository.GetAsync(
            new UserByExternalIdSpecification(externalId), cancellationToken)).FirstOrDefault();

        Subscription? newlyCreatedSubscription = null;

        if (user is null)
        {
            user = User.CreateFromExternalIdentity(externalId, email, displayName);
            await _userRepository.AddAsync(user, cancellationToken);

            newlyCreatedSubscription = Subscription.CreateFree(user.Id);
            await _subscriptionRepository.AddAsync(newlyCreatedSubscription, cancellationToken);

            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        var subscriptionRecord = newlyCreatedSubscription ?? (await _subscriptionRepository.GetAsync(
            new SubscriptionByUserIdSpecification(user.Id), cancellationToken)).FirstOrDefault();

        return Result<UserProfileDto>.Success(new UserProfileDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            subscriptionRecord?.Tier.ToString() ?? "Free"));
    }

    public async Task<Result<UserClaimsDto>> GetClaimsAsync(
        string externalId, CancellationToken cancellationToken = default)
    {
        var user = (await _userRepository.GetAsync(
            new UserByExternalIdSpecification(externalId), cancellationToken)).FirstOrDefault();

        if (user is null)
            return Result<UserClaimsDto>.Failure("User not found.");

        var subscription = (await _subscriptionRepository.GetAsync(
            new SubscriptionByUserIdSpecification(user.Id), cancellationToken)).FirstOrDefault();

        return Result<UserClaimsDto>.Success(new UserClaimsDto(
            externalId,
            user.Role.ToString(),
            subscription?.Tier.ToString() ?? "Free"));
    }
}