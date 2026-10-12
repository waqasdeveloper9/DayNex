using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain.Models;
using DayNex.VehicleService.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DayNex.Tests;

public sealed class MotPersistenceTests
{
    [Fact]
    public async Task SnapshotReplacementIsIdempotentOwnerScopedAndPreservedOnFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new VehicleDbContext(new DbContextOptionsBuilder<VehicleDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var writer = new AssetWriter(db);
        var store = new MotHistoryStore(db);
        var owner = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var vehicle = await writer.AddVehicleAsync(UserVehicle.Create(owner, "DA67RVW", now), default);

        Assert.False(await store.TryBeginAsync(Guid.NewGuid(), vehicle.Id, now, default));
        Assert.True(await store.TryBeginAsync(owner, vehicle.Id, now, default));
        Assert.False(await store.TryBeginAsync(owner, vehicle.Id, now.AddSeconds(1), default));
        Assert.True(await store.CompleteAsync(owner, vehicle.Id, now, Snapshot(vehicle.Id, now, "first"), default));

        var saved = await store.ReadAsync(owner, vehicle.Id, 1, 20, default);
        Assert.Equal("Available", saved!.Status);
        Assert.Single(saved.Tests);
        Assert.Single(saved.Tests[0].Defects);
        Assert.Null(await store.ReadAsync(Guid.NewGuid(), vehicle.Id, 1, 20, default));

        var failed = now.AddMinutes(6);
        Assert.True(await store.TryBeginAsync(owner, vehicle.Id, failed, default));
        Assert.True(await store.CompleteAsync(owner, vehicle.Id, failed, new("ProviderAccessDenied"), default));
        saved = await store.ReadAsync(owner, vehicle.Id, 1, 20, default);
        Assert.Equal("ProviderAccessDenied", saved!.Status);
        Assert.Equal("first", saved.Tests[0].MotTestNumber);
        Assert.Equal(now, saved.RetrievedUtc);

        var next = now.AddMinutes(12);
        Assert.True(await store.TryBeginAsync(owner, vehicle.Id, next, default));
        Assert.False(await store.CompleteAsync(owner, vehicle.Id, now, Snapshot(vehicle.Id, now, "stale"), default));
        Assert.True(await store.CompleteAsync(owner, vehicle.Id, next, Snapshot(vehicle.Id, next, "second"), default));
        Assert.Equal(1, await db.MotHistories.CountAsync());
        Assert.Equal(1, await db.MotTests.CountAsync());
        Assert.Equal(1, await db.Set<MotDefect>().CountAsync());
        Assert.Equal("second", (await store.ReadAsync(owner, vehicle.Id, 1, 1, default))!.Tests[0].MotTestNumber);

        Assert.True(await writer.RemoveVehicleAsync(owner, vehicle.Id, default));
        Assert.False(await store.CompleteAsync(owner, vehicle.Id, next, Snapshot(vehicle.Id, next, "late"), default));
        Assert.Equal(0, await db.MotHistories.CountAsync());
        Assert.Equal(0, await db.MotTests.CountAsync());
        Assert.Equal(0, await db.Set<MotDefect>().CountAsync());
    }

    private static MotLookupResult Snapshot(Guid vehicleId, DateTimeOffset now, string number) => new(
        "Available", new MotVehicleHistory(vehicleId)
        {
            Registration = "DA67RVW", HasOutstandingRecall = "No", RetrievedUtc = now,
            Tests =
            [
                new MotTest
                {
                    HistoryId = vehicleId, CompletedDate = now, TestResult = "PASSED",
                    DataSource = "DVSA", MotTestNumber = number, OdometerResultType = "READ",
                    Defects = [new MotDefect { Text = "Example", Type = "ADVISORY", Dangerous = false }]
                }
            ]
        });
}
