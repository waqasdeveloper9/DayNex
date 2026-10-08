using DayNex.Contracts.Identity;
using DayNex.IdentityService.Domain.Entities;
namespace DayNex.IdentityService.Application.Interface;
public interface ICustomerPreferencesStore
{
    Task<CustomerPreferences?> FindAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveAsync(CustomerPreferences preferences, CancellationToken cancellationToken);
}
public interface ICustomerPreferencesService
{
    Task<CustomerPreferencesDto> GetAsync(string externalId, CancellationToken cancellationToken);
    Task<CustomerPreferencesDto> SaveAsync(string externalId, CustomerPreferencesDto input, CancellationToken cancellationToken);
}
public sealed class CustomerProfileRequiredException() : Exception("Load your profile before saving details.");
