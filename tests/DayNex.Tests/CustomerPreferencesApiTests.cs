using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using DayNex.Contracts.Identity;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace DayNex.Tests;
public sealed class MemoryPreferencesStore : ICustomerPreferencesStore
{
    private readonly ConcurrentDictionary<Guid, CustomerPreferences> values = new();
    public Task<CustomerPreferences?> FindAsync(Guid id, CancellationToken token) => Task.FromResult(values.GetValueOrDefault(id));
    public Task SaveAsync(CustomerPreferences value, CancellationToken token) { values[value.UserId] = value; return Task.CompletedTask; }
}
public sealed class PreferencesFactory : WebApplicationFactory<DayNex.IdentityService.WebApi.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestAuthentication.Configuration));
        builder.ConfigureServices(services => {
            services.RemoveAll<IIdentityStore>(); services.RemoveAll<ICustomerPreferencesStore>();
            services.AddSingleton<IIdentityStore, MemoryIdentityStore>();
            services.AddSingleton<ICustomerPreferencesStore, MemoryPreferencesStore>();
            TestAuthentication.Configure(services);
        });
    }
}
public sealed class CustomerPreferencesApiTests
{
    [Fact]
    public async Task PreferencesRequireTokenScopeAndIgnoreRequestedUserIdentity()
    {
        await using var factory = new PreferencesFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        const string path = "/api/identity/me/preferences";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid(), "wrong"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(path, new CustomerPreferencesDto(Postcode: "SW1A 1AA"))).StatusCode);
        var first = TestAuthentication.Token(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new("Bearer", first);
        Assert.False((await client.GetFromJsonAsync<CustomerPreferencesDto>(path))!.OnboardingCompleted);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, new CustomerPreferencesDto(Postcode: "SW1A 1AA"))).StatusCode);
        (await client.GetAsync("/api/identity/me")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path, new CustomerPreferencesDto(Postcode: "bad"))).StatusCode);
        var saved = await client.PutAsJsonAsync(path + "?externalId=another&userId=another", new CustomerPreferencesDto(Postcode: "SW1A1AA"));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid()));
        (await client.GetAsync("/api/identity/me")).EnsureSuccessStatusCode();
        var secondPreferences = await client.GetFromJsonAsync<CustomerPreferencesDto>(path);
        Assert.False(secondPreferences!.OnboardingCompleted);
        client.DefaultRequestHeaders.Authorization = new("Bearer", first);
        Assert.Equal("SW1A 1AA", (await client.GetFromJsonAsync<CustomerPreferencesDto>(path))!.Postcode);
    }
}
