using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Persistence.Repositories;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DayNex.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAYNEX_TEST_SQLSERVER")))
            Skip = "Set DAYNEX_TEST_SQLSERVER to a disposable local SQL Server connection to run database race tests.";
    }
}

public sealed class SqlServerTests
{
    private static async Task WithDatabase(Func<string, Task> test)
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("DAYNEX_TEST_SQLSERVER"))
        { InitialCatalog = "master", Pooling = false };
        var name = "DayNexTests_" + Guid.NewGuid().ToString("N");
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using (var create = master.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE [{name}]";
            await create.ExecuteNonQueryAsync();
        }
        builder.InitialCatalog = name;
        try { await test(builder.ConnectionString); }
        finally
        {
            // name is generated here, never taken from a connection string or user data.
            await using var drop = master.CreateCommand();
            drop.CommandText = $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]";
            await drop.ExecuteNonQueryAsync();
        }
    }

    [SqlServerFact]
    public async Task SqlServerMigrationsAndConcurrentProvisioningCreateOneAccountAndSubscription()
    {
        await WithDatabase(async connection =>
        {
            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3)).Options;
            await using (var setup = new IdentityDbContext(options)) await setup.Database.MigrateAsync();
            var identity = $"{TestAuthentication.Tenant:D}:{Guid.NewGuid():D}";
            var tasks = Enumerable.Range(0, 20).Select(async _ =>
            {
                await using var context = new IdentityDbContext(options);
                return await new IdentityStore(context).ProvisionAsync(identity, null, null, CancellationToken.None);
            });
            var records = await Task.WhenAll(tasks);
            Assert.Single(records.Select(r => r.User.Id).Distinct());
            await using var verification = new IdentityDbContext(options);
            Assert.Equal(1, await verification.Users.CountAsync());
            Assert.Equal(1, await verification.Subscriptions.CountAsync());
            Assert.False(verification.Database.HasPendingModelChanges());
        });
    }

    [SqlServerFact]
    public async Task SqlServerMigrationsAndConcurrentHolidaySyncDoNotDuplicateRecords()
    {
        await WithDatabase(async connection =>
        {
            var options = new DbContextOptionsBuilder<HolidayDbContext>()
                .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3)).Options;
            await using (var setup = new HolidayDbContext(options)) await setup.Database.MigrateAsync();
            var tasks = Enumerable.Range(0, 4).Select(async _ =>
            {
                await using var context = new HolidayDbContext(options);
                return await new HolidaySyncStore(context).SynchronizeAsync(
                    [new BankHoliday(Region.Scotland, new DateOnly(2026, 12, 25), "Christmas Day", null, true)],
                    CancellationToken.None);
            });
            await Task.WhenAll(tasks);
            await using var verification = new HolidayDbContext(options);
            Assert.Equal(1, await verification.BankHoliday.CountAsync());
            Assert.False(verification.Database.HasPendingModelChanges());
        });
    }
}
