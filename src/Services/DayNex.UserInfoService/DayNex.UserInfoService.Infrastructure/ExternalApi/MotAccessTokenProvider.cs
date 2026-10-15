using System.Text.Json;
using System.Text.Json.Serialization;
using DayNex.Shared.Http.Interface;
using Microsoft.Extensions.Options;

namespace DayNex.UserInfoService.Infrastructure;

/// <summary>App-only DVSA token cache. This never uses a customer's Google/Entra token.</summary>
public sealed class MotAccessTokenProvider(
    IApiClient apiClient, IOptions<MotOptions> options, TimeProvider clock) : IDisposable
{
    private sealed record CachedToken(string Value, DateTimeOffset RefreshUtc);
    private CachedToken? _cached;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        var cached = Volatile.Read(ref _cached);
        if (cached is not null && cached.RefreshUtc > clock.GetUtcNow()) return cached.Value;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            cached = _cached;
            if (cached is not null && cached.RefreshUtc > clock.GetUtcNow()) return cached.Value;

            var settings = options.Value;
            var requestedUtc = clock.GetUtcNow();
            MotTokenResponse? response;
            try
            {
                response = await apiClient.PostFormAsync<MotTokenResponse>(settings.TokenEndpoint,
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = settings.ClientId,
                        ["client_secret"] = settings.ClientSecret,
                        ["scope"] = settings.Scope
                    }, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException ||
                exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                // Do not propagate provider response bodies, client secrets or access tokens.
                throw new MotAuthenticationException();
            }

            if (response is null || string.IsNullOrWhiteSpace(response.AccessToken) ||
                !string.Equals(response.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) ||
                response.ExpiresIn is < 60 or > 86400)
                throw new MotAuthenticationException();

            cached = new CachedToken(response.AccessToken,
                requestedUtc.AddSeconds(response.ExpiresIn - Math.Min(60, response.ExpiresIn / 5)));
            Volatile.Write(ref _cached, cached);
            return cached.Value;
        }
        finally { _gate.Release(); }
    }

    public void Dispose() => _gate.Dispose();
}

public sealed class MotAuthenticationException : Exception
{
    public MotAuthenticationException() : base("DVSA application authentication is unavailable.") { }
}

public sealed record MotTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
