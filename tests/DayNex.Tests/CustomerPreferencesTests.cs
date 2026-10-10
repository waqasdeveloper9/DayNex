using DayNex.Contracts.Identity;
using DayNex.IdentityService.Domain.Entities;
using DayNex.IdentityService.Application.Services;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace DayNex.Tests;
public sealed class CustomerPreferencesTests
{
    [Theory]
    [InlineData("sw1a1aa", "SW1A 1AA")]
    [InlineData("eh1 1yz", "EH1 1YZ")]
    [InlineData("gir0aa", "GIR 0AA")]
    public void DetailsAreNormalized(string input, string expected)
    {
        var value = CustomerPreferences.Create(Guid.NewGuid(), input, "Scotland", DateTime.UtcNow);
        Assert.Equal(expected, value.Postcode);
    }
    [Theory]
    [InlineData("bad", "EnglandAndWales")]
    [InlineData("SW1A 1AA", "Unknown")]
    public void InvalidDetailsAreRejected(string postcode,string region) =>
        Assert.Throws<ArgumentException>(()=>CustomerPreferences.Create(Guid.NewGuid(),postcode,region,DateTime.UtcNow));
    [Fact]
    public void PersonalDetailsCanBeSkipped() =>
        Assert.Equal("",CustomerPreferences.Create(Guid.NewGuid(),"","EnglandAndWales",DateTime.UtcNow).Postcode);
    [Fact]
    public async Task PreferencesPersistAreIsolatedAndUpdatesDoNotCreateDuplicates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var identities = new IdentityStore(context);
        await identities.ProvisionAsync("tenant:first", null, null, default);
        await identities.ProvisionAsync("tenant:second", null, null, default);
        var service = new CustomerPreferencesService(identities, new CustomerPreferencesStore(context), TimeProvider.System);
        Assert.False((await service.GetAsync("tenant:first", default)).OnboardingCompleted);
        var first = await service.SaveAsync("tenant:first", new(Postcode: "SW1A1AA"), default);
        Assert.True(first.OnboardingCompleted);
        Assert.False((await service.GetAsync("tenant:second", default)).OnboardingCompleted);
        await service.SaveAsync("tenant:first", new(Postcode: "EH1 1YZ", HolidayRegion: "Scotland"), default);
        Assert.Equal("Scotland", (await service.GetAsync("tenant:first", default)).HolidayRegion);
        Assert.Equal(1, await context.Set<CustomerPreferences>().CountAsync());
        context.Users.Remove(await context.Users.SingleAsync(u => u.ExternalId == "tenant:first"));
        await context.SaveChangesAsync();
        Assert.Equal(0, await context.Set<CustomerPreferences>().CountAsync());
    }
    [Fact]
    public async Task HolidayReadIsBoundedOrderedAndRegionSpecific()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateCollation("Latin1_General_100_BIN2", string.CompareOrdinal);
        await using var context = new HolidayDbContext(new DbContextOptionsBuilder<HolidayDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var today = new DateOnly(2026, 10, 7);
        context.BankHoliday.AddRange(
            new BankHoliday(Region.Scotland, today.AddDays(-1), "Past", null, false),
            new BankHoliday(Region.Scotland, today.AddDays(2), "Later", null, false),
            new BankHoliday(Region.Scotland, today, "Today", null, false),
            new BankHoliday(Region.EnglandAndWales, today, "Other region", null, false));
        await context.SaveChangesAsync();
        var query = new UpcomingHolidayQuery(context, new FixedClock(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero)));
        var result = await query.GetAsync(Region.Scotland, 1, default);
        Assert.Equal("Today", Assert.Single(result.Holidays).Title);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.GetAsync(Region.Scotland, 1000, default));
    }
}
