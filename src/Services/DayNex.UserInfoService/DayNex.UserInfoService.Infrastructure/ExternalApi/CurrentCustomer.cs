using DayNex.Contracts.Identity;
using DayNex.Shared.Http.Interface;
using DayNex.UserInfoService.Application;
using Microsoft.Extensions.Options;

namespace DayNex.UserInfoService.Infrastructure;
public sealed class CurrentCustomer : ICurrentCustomer
{
    private readonly IApiClient _apiClient;
    private readonly UserInfoIdentityOptions _settings;
    private Guid? _userId;
    public CurrentCustomer(IApiClient apiClient, IOptions<UserInfoIdentityOptions> options)
    {
        _apiClient = apiClient;
        _settings = options.Value;
    }

    public async Task<Guid> GetUserIdAsync(CancellationToken cancellationToken)
    {
        if (_userId is { } userId)
            return userId;
        var profile = await _apiClient.GetAsync<UserProfileDto>(_settings.ProfileEndpoint, cancellationToken);
        if (profile is null || profile.Id == Guid.Empty)
            throw new HttpRequestException("Identity profile unavailable.");
        _userId = profile.Id;
        return profile.Id;
    }
}
