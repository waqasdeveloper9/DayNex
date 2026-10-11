using System.Net;
using System.Net.Http.Json;
using DayNex.Shared.Http;
using DayNex.VehicleService.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DayNex.Tests;

public sealed class VehicleIdentityClientTests
{
    [Fact]
    public async Task UsesConfiguredProfileEndpointAndCachesOnlyWithinTheInstance()
    {
        var userId = Guid.NewGuid();
        var handler = new ProfileHandler(userId);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://identity.example/") };
        var apiClient = new ApiClient(http, NullLogger<ApiClient>.Instance);
        var options = Options.Create(new VehicleIdentityOptions { ProfileEndpoint = "configured/profile" });
        var customer = new CurrentCustomer(apiClient, options);

        Assert.Equal(userId, await customer.GetUserIdAsync(default));
        Assert.Equal(userId, await customer.GetUserIdAsync(default));
        Assert.Equal("https://identity.example/configured/profile", handler.LastUri?.AbsoluteUri);
        Assert.Equal(1, handler.Calls);

        var nextRequest = new CurrentCustomer(apiClient, options);
        Assert.Equal(userId, await nextRequest.GetUserIdAsync(default));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task EmptyIdentityCannotBecomeAnOwnershipKey()
    {
        using var http = new HttpClient(new ProfileHandler(Guid.Empty))
        {
            BaseAddress = new Uri("https://identity.example/")
        };
        var customer = new CurrentCustomer(new ApiClient(http, NullLogger<ApiClient>.Instance),
            Options.Create(new VehicleIdentityOptions { ProfileEndpoint = "configured/profile" }));

        await Assert.ThrowsAsync<HttpRequestException>(() => customer.GetUserIdAsync(default));
    }

    private sealed class ProfileHandler(Guid userId) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { Id = userId })
            });
        }
    }
}