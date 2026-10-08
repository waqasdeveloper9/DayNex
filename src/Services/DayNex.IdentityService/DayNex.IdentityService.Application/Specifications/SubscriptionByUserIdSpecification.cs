using DayNex.Domain.Common.Specifications;
using DayNex.IdentityService.Domain.Entities;

namespace DayNex.IdentityService.Application.Specifications;

public sealed class SubscriptionByUserIdSpecification : BaseSpecification<Subscription>
{
    public SubscriptionByUserIdSpecification(Guid userId)
        : base(s => s.UserId == userId)
    {
    }
}