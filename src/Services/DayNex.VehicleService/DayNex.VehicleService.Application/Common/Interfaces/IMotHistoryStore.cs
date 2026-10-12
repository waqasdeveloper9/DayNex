using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;

public interface IMotHistoryStore
{
    Task<bool> TryBeginAsync(Guid userId, Guid vehicleId, DateTimeOffset leaseUtc, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid userId, Guid vehicleId, DateTimeOffset leaseUtc,
        MotLookupResult result, CancellationToken cancellationToken);
    Task<MotHistoryDto?> ReadAsync(Guid userId, Guid vehicleId, int page, int pageSize,
        CancellationToken cancellationToken);
}
