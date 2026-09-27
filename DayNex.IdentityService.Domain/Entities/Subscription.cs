using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Domain.Enums;

namespace DayNex.IdentityService.Domain.Entities;

public sealed class Subscription : IEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public DateTime? EndDateUtc { get; private set; }

    private Subscription() { }

    public static Subscription CreateFree(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        return new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Tier = SubscriptionTier.Free,
            EndDateUtc = null
        };
    }

    public void UpgradeToPremium(DateTime? endDateUtc)
    {
        Tier = SubscriptionTier.Premium;
        EndDateUtc = endDateUtc;
    }

    public void DowngradeToFree()
    {
        Tier = SubscriptionTier.Free;
        EndDateUtc = null;
    }

    public bool HasExpired(DateTime utcNow) =>
        Tier == SubscriptionTier.Premium && EndDateUtc.HasValue && EndDateUtc.Value < utcNow;
}