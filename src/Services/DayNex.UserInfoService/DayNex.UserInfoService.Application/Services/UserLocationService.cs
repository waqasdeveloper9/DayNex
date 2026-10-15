using DayNex.UserInfoService.Domain.Models;
namespace DayNex.UserInfoService.Application;
public sealed class UserLocationService(ICurrentCustomer customer, IUserLocationStore store, ICouncilClient client, TimeProvider clock)
{
    public async Task<LocationDto?> GetAsync(CancellationToken token) => Map(await store.FindAsync(await customer.GetUserIdAsync(token), token));
    public async Task<LocationDto> SaveAsync(string? postcode, CancellationToken token)
    {
        var normalized = UserLocation.Normalize(postcode);
        var userId = await customer.GetUserIdAsync(token);
        var row = await store.SetPostcodeAsync(userId, normalized, clock.GetUtcNow(), token);
        if (normalized.Length == 0) return Map(row)!;
        var attempt = clock.GetUtcNow().UtcTicks;
        if (await store.TryBeginAsync(userId, row.Revision, attempt, token))
        {
            var result = await client.FetchAsync(normalized, token);
            await store.SaveAsync(userId, row.Revision, attempt, result, clock.GetUtcNow(), token);
        }
        // Return the latest selection if another postcode update won the race.
        return Map(await store.FindAsync(userId, token))!;
    }
    private static LocationDto? Map(UserLocation? row) => row is null ? null : new(row.Postcode,
        row.LookupStatus, row.RetrievedUtc, row.Council is null ? null :
        new(row.Council.Name, row.Council.HomepageUrl, row.Council.Tier, row.Council.Slug));
}
