using DayNex.IdentityService.Application.Interfaces;

namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;

    public UnitOfWork(IdentityDbContext context, IUserRepository users, ISubscriptionRepository subscriptions)
    {
        _context = context;
        Users = users;
        Subscriptions = subscriptions;
    }

    public IUserRepository Users { get; }
    public ISubscriptionRepository Subscriptions { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
