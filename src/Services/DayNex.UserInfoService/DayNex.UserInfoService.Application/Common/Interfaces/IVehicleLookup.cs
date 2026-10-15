using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;
public interface IVehicleLookup
{
    Task<VehicleLookupResult> LookupAsync(string registration, CancellationToken token);
}
