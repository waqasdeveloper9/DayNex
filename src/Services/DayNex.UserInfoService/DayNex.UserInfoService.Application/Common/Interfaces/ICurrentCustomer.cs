using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;
public interface ICurrentCustomer
{
    Task<Guid> GetUserIdAsync(CancellationToken token);
}
