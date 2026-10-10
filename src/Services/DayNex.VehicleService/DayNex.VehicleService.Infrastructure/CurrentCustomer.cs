using DayNex.Contracts.Identity;
using DayNex.Shared.Http;
using DayNex.VehicleService.Application;

namespace DayNex.VehicleService.Infrastructure;
public sealed class CurrentCustomer(ApiClient client) : ICurrentCustomer
{
    private Guid? cached;
    public async Task<Guid> GetUserIdAsync(CancellationToken token)
    {
        if (cached is { } id)
            return id;
        var profile = await client.GetAsync<UserProfileDto>("api/identity/me", token);
        if (profile is null || profile.Id == Guid.Empty)
            throw new HttpRequestException("Identity profile unavailable.");
        cached = profile.Id;
        return profile.Id;
    }
}
