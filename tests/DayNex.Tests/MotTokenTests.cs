using System.Net;
using System.Net.Http.Json;
using DayNex.Shared.Http;
using DayNex.VehicleService.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DayNex.Tests;

public sealed class MotTokenTests
{
    [Fact]
    public async Task ConcurrentRequestsShareOneTokenAndRefreshBeforeProviderExpiry()
    {
        var handler = new TokenHandler();
        using var http = new HttpClient(handler);
        var clock = new TestClock();
        using var tokens = Create(http, clock);

        var values = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => tokens.GetAsync(default)));
        Assert.All(values, value => Assert.Equal("test-token-1", value));
        Assert.Equal(1, handler.Calls);
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Contains("grant_type=client_credentials", handler.Body);
        Assert.Contains("client_id=test-client", handler.Body);
        Assert.Contains("scope=https%3A%2F%2Ftapi.dvsa.gov.uk%2F.default", handler.Body);

        clock.Advance(TimeSpan.FromSeconds(1140));
        Assert.Equal("test-token-2", await tokens.GetAsync(default));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task AuthenticationFailureDoesNotExposeProviderBodyOrCredentials()
    {
        using var http = new HttpClient(new TokenHandler(HttpStatusCode.BadRequest));
        using var tokens = Create(http, new TestClock());
        var error = await Assert.ThrowsAsync<MotAuthenticationException>(() => tokens.GetAsync(default));
        Assert.DoesNotContain("test-secret", error.ToString());
        Assert.DoesNotContain("private-error", error.ToString());
    }

    [Fact]
    public async Task MotHeadersUseProviderTokenAndKey()
    {
        var handler = new TokenHandler();
        using var tokenHttp = new HttpClient(handler);
        using var tokens = Create(tokenHttp, new TestClock());
        var transport = new HeaderHandler();
        using var auth = new MotAuthorizationHandler(tokens,
            Options.Create(new MotOptions { ApiKey = "test-api-key" })) { InnerHandler = transport };
        using var client = new HttpClient(auth);
        await client.GetAsync("https://history.mot.api.gov.uk/v1/trade/vehicles/registration/DA67RVW");
        Assert.Equal("Bearer test-token-1", transport.Authorization);
        Assert.Equal("test-api-key", transport.ApiKey);
    }

    private static MotAccessTokenProvider Create(HttpClient http, TimeProvider clock) => new(
        new ApiClient(http, NullLogger<ApiClient>.Instance),
        Options.Create(new MotOptions
        {
            TokenEndpoint = "https://login.microsoftonline.com/test/oauth2/v2.0/token",
            ClientId = "test-client", ClientSecret = "test-secret", Scope = "https://tapi.dvsa.gov.uk/.default"
        }), clock);

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class TokenHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            ContentType = request.Content!.Headers.ContentType!.MediaType;
            Body = await request.Content.ReadAsStringAsync(token);
            return new HttpResponseMessage(status)
            {
                Content = status == HttpStatusCode.OK
                    ? JsonContent.Create(new { access_token = "test-token-" + Calls, token_type = "Bearer", expires_in = 1199 })
                    : new StringContent("private-error test-secret")
            };
        }
    }

    private sealed class HeaderHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }
        public string? ApiKey { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Authorization = request.Headers.Authorization?.ToString();
            ApiKey = request.Headers.GetValues("X-API-Key").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }
}
