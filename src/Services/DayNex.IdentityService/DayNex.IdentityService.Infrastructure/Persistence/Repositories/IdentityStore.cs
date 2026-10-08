using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DayNex.IdentityService.Infrastructure.Persistence.Repositories;

public sealed class IdentityStore(IdentityDbContext context) : IIdentityStore
{
    public Task<IdentityRecord?> FindAsync(string externalId, CancellationToken cancellationToken) =>
        (from user in context.Users.AsNoTracking()
         where user.ExternalId == externalId
         join subscription in context.Subscriptions.AsNoTracking() on user.Id equals subscription.UserId into subscriptions
         from subscription in subscriptions.DefaultIfEmpty()
         select new IdentityRecord(user, subscription)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IdentityRecord> ProvisionAsync(string externalId, string? email, string? displayName, CancellationToken cancellationToken)
    {
        var user = User.CreateFromExternalIdentity(externalId, email, displayName);
        var subscription = Subscription.CreateFree(user.Id);
        context.Users.Add(user);
        context.Subscriptions.Add(subscription);
        try
        {
            // SaveChanges is atomic. SQL's unique index arbitrates concurrent requests across instances.
            await context.SaveChangesAsync(cancellationToken);
            return new(user, subscription);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.ChangeTracker.Clear();
            var existing = await FindAsync(externalId, cancellationToken);
            if (existing is null) throw;
            return existing;
        }
    }
}
