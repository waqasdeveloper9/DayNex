using DayNex.Shared.Contracts;
using DayNex.Shared.Contracts.Enums;

namespace DayNex.IdentityService.Domain.Entities;

/// <summary>
/// One active subscription record per user. Kept inside IdentityService (not a
/// separate microservice) by deliberate choice for now — payment-provider integration
/// stays a swap of Infrastructure later, without splitting the service.
/// </summary>
public class Subscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDateUtc { get; private set; }
    public DateTime? EndDateUtc { get; private set; }
    public string? PaymentProviderRef { get; private set; } // populated once billing is wired up

    private Subscription() { } // EF Core

    private Subscription(Guid userId, SubscriptionTier tier, SubscriptionStatus status, DateTime? endDateUtc)
    {
        UserId = userId;
        Tier = tier;
        Status = status;
        StartDateUtc = DateTime.UtcNow;
        EndDateUtc = endDateUtc;
    }

    public static Subscription CreateFree(Guid userId)
        => new(userId, SubscriptionTier.Free, SubscriptionStatus.Active, endDateUtc: null);

    public static Subscription CreateTrialPremium(Guid userId, int trialDays = 14)
        => new(userId, SubscriptionTier.Premium, SubscriptionStatus.Trial, DateTime.UtcNow.AddDays(trialDays));

    public void UpgradeToPremium(DateTime? endDateUtc = null)
    {
        Tier = SubscriptionTier.Premium;
        Status = SubscriptionStatus.Active;
        EndDateUtc = endDateUtc;
        MarkUpdated();
    }

    public void Downgrade()
    {
        Tier = SubscriptionTier.Free;
        Status = SubscriptionStatus.Active;
        EndDateUtc = null;
        MarkUpdated();
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        MarkUpdated();
    }

    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
        MarkUpdated();
    }

    public void AttachPaymentReference(string providerRef)
    {
        PaymentProviderRef = providerRef;
        MarkUpdated();
    }

    /// <summary>Effective access check — expired/cancelled premium behaves like Free.</summary>
    public bool IsEffectivelyPremium()
        => Tier == SubscriptionTier.Premium
           && (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial)
           && (EndDateUtc is null || EndDateUtc > DateTime.UtcNow);
}
