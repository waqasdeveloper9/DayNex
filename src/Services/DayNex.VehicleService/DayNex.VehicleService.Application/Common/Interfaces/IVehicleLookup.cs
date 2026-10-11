using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;
public interface IVehicleLookup
{
    Task<VehicleLookupResult> LookupAsync(string registration, CancellationToken token);
}
