using DayNex.IdentityService.Domain.Entities;
using DayNex.IdentityService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    }
}