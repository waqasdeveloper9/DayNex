using DayNex.VehicleService.Domain.Models;
namespace DayNex.VehicleService.Application;
public interface IVehicleRefreshJobStore
{
    Task<bool> AcquireAsync(Guid leaseId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserVehicle>> ReadDueAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);
    Task<bool> MarkAttemptAsync(Guid leaseId, Guid vehicleId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> HasMoreAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task CompleteAsync(VehicleRefreshBatchResult result, CancellationToken cancellationToken);
}

