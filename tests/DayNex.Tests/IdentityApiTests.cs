using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DayNex.Tests;

public sealed class MemoryIdentityStore : IIdentityStore
{
    public ConcurrentDictionary<string, IdentityRecord> Records { get; } = new();
    public Task<IdentityRecord?> FindAsync(string externalId, CancellationToken cancellationToken) =>
        Task.FromResult(Records.GetValueOrDefault(externalId));
    public Task<IdentityRecord> ProvisionAsync(string externalId, string? email, string? displayName, CancellationToken cancellationToken) =>
        Task.FromResult(Records.GetOrAdd(externalId, key =>
        {
            var user = User.CreateFromExternalIdentity(key, email, displayName);
            return new(user, Subscription.CreateFree(user.Id));
        }));
}

public sealed class IdentityFactory : WebApplicationFactory<DayNex.IdentityService.WebApi.Program>
{
    public MemoryIdentityStore Store { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestAuthentication.Configuration));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIdentityStore>();
            services.AddSingleton<IIdentityStore>(Store);
            TestAuthentication.Configure(services);
        });
    }
}

public sealed class IdentityApiTests : IClassFixture<IdentityFactory>
{
    private readonly IdentityFactory _factory;
    private readonly HttpClient _client;
    public IdentityApiTests(IdentityFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }

    private async Task<HttpResponseMessage> Get(string? token, string path = "/api/identity/me")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task MissingTokenIsUnauthorized() => Assert.Equal(HttpStatusCode.Unauthorized, (await Get(null)).StatusCode);

    [Theory]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("expiry")]
    [InlineData("signature")]
    [InlineData("identity")]
    [InlineData("tenant")]
    public async Task InvalidTokenIsUnauthorized(string fault)
    {
        using var rsa = RSA.Create(2048);
        var token = TestAuthentication.Token(Guid.NewGuid(),
            audience: fault == "audience" ? Guid.NewGuid().ToString() : null,
            issuer: fault == "issuer" ? "https://wrong.test" : null,
            expired: fault == "expiry", key: fault == "signature" ? new RsaSecurityKey(rsa) : null,
            includeIdentity: fault != "identity", tenant: fault == "tenant" ? Guid.NewGuid() : null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(token)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("other_scope")]
    [InlineData("prefix_access_as_user")]
    public async Task MissingRequiredScopeIsForbidden(string? scope) =>
        Assert.Equal(HttpStatusCode.Forbidden, (await Get(TestAuthentication.Token(Guid.NewGuid(), scope))).StatusCode);

    [Fact]
    public async Task LoginUsesTokenIdentityAndCreatesOnlyOneFreeSubscription()
    {
        var objectId = Guid.NewGuid();
        var token = TestAuthentication.Token(objectId);
        var profile = await (await Get(token, "/api/identity/me?externalId=attacker&email=attacker@test&displayName=Fake"))
            .Content.ReadFromJsonAsync<UserProfileDto>();
        var repeated = await (await Get(token)).Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(profile.Id, repeated!.Id);
        Assert.Equal("person@example.test", profile.Email);
        Assert.Equal("User", profile.Role);
        Assert.Equal("Free", profile.SubscriptionTier);
        Assert.True(_factory.Store.Records.ContainsKey($"{TestAuthentication.Tenant:D}:{objectId:D}"));
        Assert.False(_factory.Store.Records.ContainsKey("attacker"));
    }

    [Fact]
    public async Task MissingOptionalProfileClaimsStillAllowsLogin()
    {
        var profile = await (await Get(TestAuthentication.Token(Guid.NewGuid(), includeProfile: false)))
            .Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Null(profile.Email);
        Assert.Equal("DayNex user", profile.DisplayName);
    }

    [Fact]
    public async Task DifferentUsersWithSameEmailAreSeparateAccounts()
    {
        var first = await (await Get(TestAuthentication.Token(Guid.NewGuid()))).Content.ReadFromJsonAsync<UserProfileDto>();
        var second = await (await Get(TestAuthentication.Token(Guid.NewGuid()))).Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotEqual(first!.Id, second!.Id);
    }

    [Fact]
    public async Task PermissionsAreSelfOnlyAndDoNotProvisionUsers()
    {
        var token = TestAuthentication.Token(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await Get(token, "/api/identity/me/permissions")).StatusCode);
        await Get(token);
        var response = await Get(token, "/api/identity/me/permissions?externalId=another-user");
        var permissions = await response.Content.ReadFromJsonAsync<UserPermissionsDto>();
        Assert.Empty(permissions!.Permissions);
        Assert.Equal(HttpStatusCode.NotFound, (await Get(token, "/api/identity/internal/claims/another-user")).StatusCode);
    }

    [Fact]
    public async Task SimultaneousRequestsReturnTheSameUser()
    {
        var objectId = Guid.NewGuid();
        var token = TestAuthentication.Token(objectId);
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Get(token)));
        var profiles = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<UserProfileDto>()));
        Assert.Single(profiles.Select(p => p!.Id).Distinct());
    }
}
