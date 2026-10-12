using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DayNex.VehicleService.Infrastructure;

public sealed class MotHistoryStore(VehicleDbContext db) : IMotHistoryStore
{
    public async Task<bool> TryBeginAsync(Guid userId, Guid vehicleId, DateTimeOffset leaseUtc,
        CancellationToken cancellationToken)
    {
        var cutoff = leaseUtc - TimeSpan.FromMinutes(5);
        return await db.Vehicles.Where(vehicle => vehicle.Id == vehicleId && vehicle.UserId == userId &&
            (vehicle.MotLastAttemptUtc == null || vehicle.MotLastAttemptUtc <= cutoff))
            .ExecuteUpdateAsync(update => update
                .SetProperty(vehicle => vehicle.MotLastAttemptUtc, leaseUtc)
                .SetProperty(vehicle => vehicle.MotLookupStatus, "Pending"), cancellationToken) > 0;
    }

    public Task<bool> CompleteAsync(Guid userId, Guid vehicleId, DateTimeOffset leaseUtc,
        MotLookupResult result, CancellationToken cancellationToken)
    {
        if (result.History is { } history && history.Id != vehicleId)
            throw new ArgumentException("MOT snapshot belongs to a different vehicle.");

        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var history = result.History;
            var retrievedUtc = history?.RetrievedUtc;
            var updated = await db.Vehicles.Where(vehicle => vehicle.Id == vehicleId &&
                vehicle.UserId == userId && vehicle.MotLastAttemptUtc == leaseUtc)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(vehicle => vehicle.MotLookupStatus, result.Status)
                    .SetProperty(vehicle => vehicle.MotRetrievedUtc,
                        vehicle => retrievedUtc ?? vehicle.MotRetrievedUtc),
                    cancellationToken);
            if (updated == 0) return false;

            if (history is not null)
            {
                // Replace the complete snapshot atomically, including corrected/deleted provider tests.
                // Cascades remove old tests/defects. No accumulated duplicate history rows.
                await db.MotHistories.Where(snapshot => snapshot.Id == vehicleId)
                    .ExecuteDeleteAsync(cancellationToken);
                db.MotHistories.Add(history);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<MotHistoryDto?> ReadAsync(Guid userId, Guid vehicleId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var owner = await db.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.UserId == userId && vehicle.Id == vehicleId)
            .Select(vehicle => new { vehicle.MotLookupStatus, vehicle.MotRetrievedUtc, vehicle.MotHistory })
            .SingleOrDefaultAsync(cancellationToken);
        if (owner is null) return null;

        var history = owner.MotHistory;
        var tests = db.MotTests.AsNoTracking().Where(test => test.HistoryId == vehicleId);
        var total = await tests.CountAsync(cancellationToken);
        var rows = await tests.OrderByDescending(test => test.CompletedDate).ThenBy(test => test.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Include(test => test.Defects)
            .AsSplitQuery().ToListAsync(cancellationToken);

        return new MotHistoryDto(vehicleId, owner.MotLookupStatus, owner.MotRetrievedUtc,
            history?.Registration, history?.Make, history?.Model, history?.FirstUsedDate,
            history?.FuelType, history?.PrimaryColour, history?.RegistrationDate,
            history?.ManufactureDate, history?.ManufactureYear, history?.EngineSize,
            history?.MotTestDueDate, history?.HasOutstandingRecall,
            page, pageSize, total, rows.Select(test => new MotTestDto(
                test.RegistrationAtTimeOfTest, test.CompletedDate, test.TestResult, test.ExpiryDate,
                test.OdometerValue, test.OdometerUnit, test.OdometerResultType,
                test.MotTestNumber, test.DataSource, test.Location,
                test.Defects.Select(defect => new MotDefectDto(defect.Text, defect.Type, defect.Dangerous))
                    .ToArray())).ToArray());
    }
}
