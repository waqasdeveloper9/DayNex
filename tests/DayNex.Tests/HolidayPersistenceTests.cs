using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DayNex.Tests;

public sealed class HolidayPersistenceTests
{
    [Fact]
    public async Task RepeatedSyncIsIdempotentAndUpdatesExistingHolidayDetails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateCollation("Latin1_General_100_BIN2", StringComparer.Ordinal.Compare);
        await using var context = new HolidayDbContext(new DbContextOptionsBuilder<HolidayDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var store = new HolidaySyncStore(context);
        var date = new DateOnly(2026, 12, 25);
        BankHoliday Make(string? notes) => new(Region.EnglandAndWales, date, "Christmas Day", notes, true);
        Assert.Equal(1, await store.SynchronizeAsync([Make(null), Make(null)], CancellationToken.None));
        Assert.Equal(0, await store.SynchronizeAsync([Make("Updated")], CancellationToken.None));
        var holiday = await context.BankHoliday.SingleAsync();
        Assert.Equal("Updated", holiday.Notes);
    }
}
