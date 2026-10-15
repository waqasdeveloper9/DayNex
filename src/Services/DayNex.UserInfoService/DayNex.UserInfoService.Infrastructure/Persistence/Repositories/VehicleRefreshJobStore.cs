using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace DayNex.UserInfoService.Infrastructure;

public sealed class VehicleRefreshJobStore(UserInfoDbContext db) : IVehicleRefreshJobStore
{
    public async Task<bool> AcquireAsync(Guid leaseId, DateTimeOffset now, CancellationToken token) =>
        await db.Set<VehicleRefreshJobState>().Where(j => j.Id == 1 && j.LeaseUntilUtcTicks <= now.UtcTicks)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LeaseId, leaseId)
                .SetProperty(j => j.LeaseUntilUtcTicks, now.AddMinutes(10).UtcTicks), token) == 1;

    private IQueryable<UserVehicle> Due(DateTimeOffset now)
    {
        var cutoff = now.UtcDateTime.Date.Ticks;
        var recent = now.AddMinutes(-5);
        return db.Vehicles.Where(v => (v.ScheduledAttemptUtcTicks == null || v.ScheduledAttemptUtcTicks < cutoff)
            && (v.LastAttemptUtc == null || v.LastAttemptUtc <= recent));
    }

    public async Task<IReadOnlyList<UserVehicle>> ReadDueAsync(DateTimeOffset now, int batchSize, CancellationToken token) =>
        await Due(now).AsNoTracking().OrderBy(v => v.ScheduledAttemptUtcTicks).ThenBy(v => v.Id)
            .Take(batchSize).ToListAsync(token);

    public async Task<bool> MarkAttemptAsync(Guid leaseId, Guid vehicleId, DateTimeOffset now, CancellationToken token) =>
        await db.Vehicles.Where(v => v.Id == vehicleId && db.Set<VehicleRefreshJobState>()
            .Any(j => j.Id == 1 && j.LeaseId == leaseId && j.LeaseUntilUtcTicks > now.UtcTicks))
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.ScheduledAttemptUtcTicks, now.UtcTicks), token) == 1;

    public Task<bool> HasMoreAsync(DateTimeOffset now, CancellationToken token) => Due(now).AnyAsync(token);

    public async Task CompleteAsync(VehicleRefreshBatchResult result, CancellationToken token) =>
        await db.Set<VehicleRefreshJobState>().Where(j => j.Id == 1 && j.LeaseId == result.BatchId)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LeaseId, (Guid?)null)
                .SetProperty(j => j.LeaseUntilUtcTicks, 0L)
                .SetProperty(j => j.CompletedUtcTicks, result.CompletedUtc.UtcTicks)
                .SetProperty(j => j.Processed, result.Processed).SetProperty(j => j.Failed, result.Failed)
                .SetProperty(j => j.Skipped, result.Skipped), token);
}

