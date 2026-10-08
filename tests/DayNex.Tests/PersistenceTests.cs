using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Application.Services;
using DayNex.IdentityService.Domain.Entities;
using DayNex.IdentityService.Domain.Enums;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DayNex.Tests;

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public sealed class PersistenceTests
{
    [Fact]
    public async Task UserAndSubscriptionPersistWithForeignKeyAndAllowSharedOrMissingEmail()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options;
        await using var context = new IdentityDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var store = new IdentityStore(context);
        var first = await store.ProvisionAsync("tenant:first", "same@example.test", "First", CancellationToken.None);
        await store.ProvisionAsync("tenant:second", "same@example.test", "Second", CancellationToken.None);
        await store.ProvisionAsync("tenant:third", null, null, CancellationToken.None);
        Assert.Equal(3, await context.Users.CountAsync());
        Assert.Equal(3, await context.Subscriptions.CountAsync());
        Assert.Equal(first.User.Id, (await store.FindAsync("tenant:first", CancellationToken.None))!.Subscription!.UserId);
        context.Subscriptions.Add(Subscription.CreateFree(Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FailedSubscriptionInsertRollsBackTheUserInsert()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var user = User.CreateFromExternalIdentity("tenant:user", null, null);
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSubscriptions BEFORE INSERT ON Subscriptions BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        context.Users.Add(user);
        context.Subscriptions.Add(Subscription.CreateFree(user.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Equal(0, await context.Users.CountAsync());
        Assert.Equal(0, await context.Subscriptions.CountAsync());
    }

    [Theory]
    [InlineData(-1, "Free")]
    [InlineData(0, "Free")]
    [InlineData(1, "Premium")]
    public async Task ProfileUsesEffectiveSubscriptionTier(int expiryOffsetSeconds, string expected)
    {
        var now = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var store = new MemoryIdentityStore();
        var record = await store.ProvisionAsync("user", null, null, CancellationToken.None);
        record.Subscription!.UpgradeToPremium(now.UtcDateTime.AddSeconds(expiryOffsetSeconds));
        var profile = await new UserProfileService(store, new FixedClock(now)).GetOrProvisionAsync("user", null, null);
        Assert.Equal(expected, profile.SubscriptionTier);
    }

    [Theory]
    [InlineData(UserRole.User, false)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.SuperAdmin, true)]
    public async Task OnlyDatabaseAdministratorsReceiveSyncPermission(UserRole role, bool allowed)
    {
        var store = new MemoryIdentityStore();
        var record = await store.ProvisionAsync("user", null, null, CancellationToken.None);
        record.User.ChangeRole(role);
        record.Subscription!.UpgradeToPremium(null);
        var permissions = await new UserProfileService(store, TimeProvider.System).GetPermissionsAsync("user");
        Assert.Equal(allowed, permissions!.Permissions.Contains(DayNexPermissions.HolidaySync));
    }

    [Fact]
    public async Task RoleRevocationIsVisibleOnTheNextPermissionLookup()
    {
        var store = new MemoryIdentityStore();
        var record = await store.ProvisionAsync("user", null, null, CancellationToken.None);
        record.User.ChangeRole(UserRole.Admin);
        var service = new UserProfileService(store, TimeProvider.System);
        Assert.Contains(DayNexPermissions.HolidaySync, (await service.GetPermissionsAsync("user"))!.Permissions);
        record.User.ChangeRole(UserRole.User);
        Assert.Empty((await service.GetPermissionsAsync("user"))!.Permissions);
    }
}
