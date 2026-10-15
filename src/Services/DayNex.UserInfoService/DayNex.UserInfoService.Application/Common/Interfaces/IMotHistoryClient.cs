namespace DayNex.UserInfoService.Application;

public interface IMotHistoryClient
{
    Task<MotLookupResult> FetchAsync(Guid vehicleId, string registration, CancellationToken cancellationToken);
}
