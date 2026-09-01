using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context) { }

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => await DbSet.Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Email == email.ToLower(), cancellationToken);

    public async Task<User?> GetWithSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
}
