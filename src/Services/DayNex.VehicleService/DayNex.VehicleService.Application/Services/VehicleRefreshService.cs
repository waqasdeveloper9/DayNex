using DayNex.VehicleService.Domain.Models;
namespace DayNex.VehicleService.Application;

/// <summary>Shared use case for customer requests and scheduled refreshes.</summary>
public sealed class VehicleRefreshService(IAssetWriter writer, IVehicleLookup lookup,
    VehicleMotService mot, TimeProvider clock)
{
    public async Task<bool> RefreshAsync(UserVehicle row, CancellationToken cancellationToken)
    {
        var lease = clock.GetUtcNow();
        if (!await writer.TryBeginLookupAsync(row.UserId, row.Id, lease, cancellationToken))
            throw new LookupRateLimitedException();

        var result = await lookup.LookupAsync(row.RegistrationNumber, cancellationToken);
        if (result.Details is not null) row.Apply(result.Details, clock.GetUtcNow());
        else row.RecordAttempt(result.Status, clock.GetUtcNow());
        if (!await writer.SaveLookupAsync(row, cancellationToken)) throw new AssetNotFoundException();
        var motStatus = await mot.RefreshAsync(row.UserId, row.Id, row.RegistrationNumber, cancellationToken);
        return result.Status == "Available" && motStatus == "Available";
    }
}

