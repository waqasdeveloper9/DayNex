using DayNex.UserInfoService.Domain.Models;
namespace DayNex.UserInfoService.Application;
public sealed record CouncilLookupResult(string Status, UserCouncil? Council = null);
public interface ICouncilClient
{
    Task<CouncilLookupResult> FetchAsync(string postcode, CancellationToken token);
}
public interface IUserLocationStore
{
    Task<UserLocation?> FindAsync(Guid userId, CancellationToken token);
    Task<UserLocation> SetPostcodeAsync(Guid userId, string postcode, DateTimeOffset now, CancellationToken token);
    Task<bool> TryBeginAsync(Guid userId, Guid revision, long attempt, CancellationToken token);
    Task<bool> SaveAsync(Guid userId, Guid revision, long attempt, CouncilLookupResult result, DateTimeOffset now, CancellationToken token);
}
