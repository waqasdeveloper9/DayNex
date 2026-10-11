using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;
public interface ICurrentCustomer
{
    Task<Guid> GetUserIdAsync(CancellationToken token);
}
