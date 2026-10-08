using DayNex.Domain.Common.Specifications;
using DayNex.IdentityService.Domain.Entities;

namespace DayNex.IdentityService.Application.Specifications
{
    public sealed class UserByExternalIdSpecification : BaseSpecification<User>
    {
        public UserByExternalIdSpecification(string externalId)
            : base(u => u.ExternalId == externalId)
        {
        }
    }
}
