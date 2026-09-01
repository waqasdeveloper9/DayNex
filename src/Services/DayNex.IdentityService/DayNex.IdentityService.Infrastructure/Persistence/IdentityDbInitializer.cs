using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DayNex.IdentityService.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds exactly one SuperAdmin account, read from
/// configuration ("SuperAdmin:ExternalId"/"Email"/"DisplayName") — never exposed via any
/// API endpoint. This is the only way a SuperAdmin ever gets created.
/// </summary>
public static class IdentityDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentityDbInitializer");

        await context.Database.MigrateAsync();

        var externalId = configuration["SuperAdmin:ExternalId"];
        var email = configuration["SuperAdmin:Email"];
        var displayName = configuration["SuperAdmin:DisplayName"] ?? "Super Admin";

        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("SuperAdmin:ExternalId/Email not configured — skipping SuperAdmin seed.");
            return;
        }

        var exists = await context.Users.AnyAsync(u => u.ExternalId == externalId);
        if (exists)
        {
            return;
        }

        var superAdmin = User.CreateSuperAdmin(externalId, email, displayName);
        var subscription = Subscription.CreateFree(superAdmin.Id); // tier is irrelevant — SuperAdmin bypasses tier checks
        superAdmin.AttachSubscription(subscription);

        context.Users.Add(superAdmin);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded SuperAdmin account for {Email}", email);
    }
}
