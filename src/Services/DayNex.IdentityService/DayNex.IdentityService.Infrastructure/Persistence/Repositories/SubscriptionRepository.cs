using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(IdentityDbContext context) : base(context) { }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
}
