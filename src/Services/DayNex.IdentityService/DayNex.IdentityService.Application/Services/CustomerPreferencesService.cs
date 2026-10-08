using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Entities;
namespace DayNex.IdentityService.Application.Services;
public sealed class CustomerPreferencesService(IIdentityStore identities, ICustomerPreferencesStore preferences,
    TimeProvider clock) : ICustomerPreferencesService
{
    private async Task<Guid> UserIdAsync(string externalId, CancellationToken token) =>
        (await identities.FindAsync(externalId, token))?.User.Id
        ?? throw new CustomerProfileRequiredException();
    public async Task<CustomerPreferencesDto> GetAsync(string externalId, CancellationToken cancellationToken)
    {
        var identity = await identities.FindAsync(externalId, cancellationToken);
        if (identity is null) return new();
        var value = await preferences.FindAsync(identity.User.Id, cancellationToken);
        return value is null ? new() : Map(value);
    }
    public async Task<CustomerPreferencesDto> SaveAsync(string externalId, CustomerPreferencesDto input, CancellationToken cancellationToken)
    {
        // Ownership comes from validated identity, never a user id in the request body.
        var value = CustomerPreferences.Create(await UserIdAsync(externalId, cancellationToken),
            input.VehicleRegistration, input.CompanyNumber, input.Postcode, input.HolidayRegion, clock.GetUtcNow().UtcDateTime);
        await preferences.SaveAsync(value, cancellationToken);
        return Map(value);
    }
    private static CustomerPreferencesDto Map(CustomerPreferences value) =>
        new(value.VehicleRegistration, value.CompanyNumber, value.Postcode, value.HolidayRegion, true);
}
