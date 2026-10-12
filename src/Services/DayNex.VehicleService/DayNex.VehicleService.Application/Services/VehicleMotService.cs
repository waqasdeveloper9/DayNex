namespace DayNex.VehicleService.Application;

/// <summary>MOT use case; the HTTP client and SQL implementation live in Infrastructure.</summary>
public sealed class VehicleMotService(
    IMotHistoryClient client, IMotHistoryStore store, TimeProvider clock)
{
    public async Task<string> RefreshAsync(Guid userId, Guid vehicleId, string registration,
        CancellationToken cancellationToken)
    {
        var leaseUtc = clock.GetUtcNow();
        if (!await store.TryBeginAsync(userId, vehicleId, leaseUtc, cancellationToken)) return "Busy";

        var result = await client.FetchAsync(vehicleId, registration, cancellationToken);
        if (!await store.CompleteAsync(userId, vehicleId, leaseUtc, result, cancellationToken))
            throw new AssetNotFoundException();
        return result.Status;
    }

    public Task<MotHistoryDto?> ReadAsync(Guid userId, Guid vehicleId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || page > 100000 || pageSize is < 1 or > 50)
            throw new ArgumentException("Use page >= 1 and pageSize between 1 and 50.");
        return store.ReadAsync(userId, vehicleId, page, pageSize, cancellationToken);
    }
}
