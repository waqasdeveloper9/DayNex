using System.Net;
using System.Net.Http.Json;
using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain.Models;
using DayNex.VehicleService.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace DayNex.Tests;
public sealed class VehicleRefreshJobTests
{
    private const string Endpoint = "/internal/jobs/vehicles/refresh";
    [Fact]
    public async Task JobRequiresSeparateCredentialAndCustomerCannotTriggerIt()
    {
        await using var factory = new VehicleFactory { EnableRefresh = true };
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(Endpoint, null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(Endpoint, null)).StatusCode);
        client.DefaultRequestHeaders.Add("X-DayNex-Job-Key", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(Endpoint, null)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-DayNex-Job-Key");
        client.DefaultRequestHeaders.Add("X-DayNex-Job-Key", VehicleFactory.JobKey);
        var response = await client.PostAsync(Endpoint, null);
        response.EnsureSuccessStatusCode();
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<VehicleRefreshBatchResult>())!.Processed);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/assets/me")).StatusCode);
    }
    [Fact]
    public async Task DisabledJobRejectsEvenValidCredential()
    {
        await using var factory = new VehicleFactory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("X-DayNex-Job-Key", VehicleFactory.JobKey);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(Endpoint, null)).StatusCode);
    }
    [Fact]
    public async Task BatchesRefreshDifferentOwnersAndDoNotRepeatWithinDay()
    {
        await using var factory = new VehicleFactory { EnableRefresh = true }; using var client = factory.Client();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VehicleDbContext>();
            db.Vehicles.AddRange(UserVehicle.Create(Guid.NewGuid(), "AB12CDE", DateTimeOffset.UtcNow),
                UserVehicle.Create(Guid.NewGuid(), "XY12ZZZ", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Add("X-DayNex-Job-Key", VehicleFactory.JobKey);
        for (var index = 0; index < 2; index++)
        {
            var response = await client.PostAsync(Endpoint, null); response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadFromJsonAsync<VehicleRefreshBatchResult>())!;
            Assert.Equal(1, result.Processed); Assert.Equal(0, result.Failed); Assert.Equal(index == 0, result.HasMore);
        }
        Assert.Equal(2, factory.Lookup.Calls); Assert.Equal(2, factory.Mot.Calls);
        var repeat = await client.PostAsync(Endpoint, null); repeat.EnsureSuccessStatusCode();
        Assert.Equal(0, (await repeat.Content.ReadFromJsonAsync<VehicleRefreshBatchResult>())!.Processed);
        Assert.Equal(2, factory.Lookup.Calls);
    }
    [Fact]
    public async Task ProviderFailureIsReportedAndDoesNotLoopForever()
    {
        await using var factory = new VehicleFactory { EnableRefresh = true }; using var client = factory.Client();
        factory.Lookup.Fail = true;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VehicleDbContext>();
            db.Vehicles.Add(UserVehicle.Create(Guid.NewGuid(), "AB12CDE", DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Add("X-DayNex-Job-Key", VehicleFactory.JobKey);
        var response = await client.PostAsync(Endpoint, null); response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<VehicleRefreshBatchResult>())!;
        Assert.Equal(1, result.Failed); Assert.False(result.HasMore); Assert.Equal(1, factory.Mot.Calls);
    }
    [Fact]
    public async Task SqlLeasePreventsOverlapAndExpiredOwnerCannotReleaseNewLease()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new VehicleDbContext(new DbContextOptionsBuilder<VehicleDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(); var store = new VehicleRefreshJobStore(db); var now = new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
        var first = Guid.NewGuid(); var next = Guid.NewGuid();
        Assert.True(await store.AcquireAsync(first, now, default));
        Assert.False(await store.AcquireAsync(next, now.AddMinutes(1), default));
        Assert.True(await store.AcquireAsync(next, now.AddMinutes(11), default));
        await store.CompleteAsync(new(first, 0, 0, 0, false, now), default);
        Assert.False(await store.AcquireAsync(Guid.NewGuid(), now.AddMinutes(12), default));
        var row = UserVehicle.Create(Guid.NewGuid(), "AB12CDE", now); db.Vehicles.Add(row); await db.SaveChangesAsync();
        Assert.False(await store.MarkAttemptAsync(first, row.Id, now.AddMinutes(12), default));
        Assert.True(await store.MarkAttemptAsync(next, row.Id, now.AddMinutes(12), default));
        Assert.False(await store.HasMoreAsync(now.AddHours(23), default));
        Assert.True(await store.HasMoreAsync(now.AddHours(25), default));
    }
}
