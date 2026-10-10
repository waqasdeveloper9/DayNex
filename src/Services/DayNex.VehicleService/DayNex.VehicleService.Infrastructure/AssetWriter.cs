using System.Data;
using System.Linq.Expressions;
using DayNex.Domain.Common.Interface;
using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DayNex.VehicleService.Infrastructure;
public sealed class AssetWriter(VehicleDbContext db) : IAssetWriter
{
    public Task<UserVehicle> AddVehicleAsync(UserVehicle value, CancellationToken token) => AddReferenceAsync(value, v => v.UserId == value.UserId && v.RegistrationNumber == value.RegistrationNumber, v => v.UserId == value.UserId, token);
    public Task<UserCompany> AddCompanyAsync(UserCompany value, CancellationToken token) => AddReferenceAsync(value, v => v.UserId == value.UserId && v.CompanyNumber == value.CompanyNumber, v => v.UserId == value.UserId, token);
    private Task<T> AddReferenceAsync<T>(T value, Expression<Func<T, bool>> key, Expression<Func<T, bool>> owner, CancellationToken token)
        where T : class, IEntity => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        // Serializable range locking also makes the per-user limit safe against concurrent inserts.
        // The execution strategy retries SQL deadlocks/transient failures around the complete transaction.
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var existing = await db.Set<T>().AsNoTracking().SingleOrDefaultAsync(key, token);
        if (existing is not null)
        {
            await transaction.CommitAsync(token);
            return existing;
        }

        if (await db.Set<T>().CountAsync(owner, token) >= 100)
            throw new ArgumentException("A maximum of 100 references of each type is supported.");
        db.Set<T>().Add(value);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        db.Entry(value).State = EntityState.Detached;
        return value;
    });
    public async Task<bool> TryBeginLookupAsync(Guid userId, Guid id, DateTimeOffset now, CancellationToken token)
    {
        var cutoff = now - TimeSpan.FromMinutes(5);
        return await db.Vehicles.Where(v => v.Id == id && v.UserId == userId && (v.LastAttemptUtc == null || v.LastAttemptUtc <= cutoff)).ExecuteUpdateAsync(s => s.SetProperty(v => v.LastAttemptUtc, now).SetProperty(v => v.LookupStatus, "Pending"), token) > 0;
    }

    public async Task<bool> SaveLookupAsync(UserVehicle value, CancellationToken token)
    {
        var current = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == value.Id && v.UserId == value.UserId, token);
        if (current is null)
            return false;
        if (current.LastAttemptUtc > value.LastAttemptUtc)
            return true;
        if (value.Details is not null && value.RetrievedUtc is { } retrieved)
            current.Apply(value.Details, retrieved);
        current.RecordAttempt(value.LookupStatus, value.LastAttemptUtc!.Value);
        try
        {
            await db.SaveChangesAsync(token);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> RemoveVehicleAsync(Guid userId, Guid id, CancellationToken token) => await db.Vehicles.Where(v => v.Id == id && v.UserId == userId).ExecuteDeleteAsync(token) > 0;
    public async Task<bool> RemoveCompanyAsync(Guid userId, Guid id, CancellationToken token) => await db.Companies.Where(v => v.Id == id && v.UserId == userId).ExecuteDeleteAsync(token) > 0;
}
