using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence;

/// <summary>
/// This microservice's own database context. Each DayNex microservice owns exactly
/// one database (no cross-service joins) — this is IdentityService's, separate from
/// HolidayService's or CompanyProfileService's.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Global soft-delete filter — every query automatically excludes IsDeleted rows.
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
