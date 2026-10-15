using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;
public interface IAssetWriter
{
    Task<UserVehicle> AddVehicleAsync(UserVehicle value, CancellationToken token);
    Task<UserCompany> AddCompanyAsync(UserCompany value, CancellationToken token);
    Task<bool> TryBeginLookupAsync(Guid userId, Guid id, DateTimeOffset now, CancellationToken token);
    Task<bool> SaveLookupAsync(UserVehicle value, CancellationToken token);
    Task<bool> RemoveVehicleAsync(Guid userId, Guid id, CancellationToken token);
    Task<bool> RemoveCompanyAsync(Guid userId, Guid id, CancellationToken token);
}
