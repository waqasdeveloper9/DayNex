using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using DayNex.Contracts.Identity;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.Shared.Http.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DayNex.Tests;

public sealed class StubPermissionSource(string[] permissions, bool unavailable = false) : IPermissionSource
{
    public Task<UserPermissionsDto?> GetAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
        unavailable ? throw new PermissionServiceUnavailableException()
        : Task.FromResult<UserPermissionsDto?>(new UserPermissionsDto(Guid.NewGuid(), permissions));
}

public sealed class StubHolidayService : IBankHoliday
{
    public int Calls { get; private set; }
    public Task<int> SyncHolidaysFromGovApiAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(3);
    }
}

public sealed class HolidayFactory(string[] permissions, bool unavailable = false)
    : WebApplicationFactory<DayNex.HolidayService.API.Program>
{
    public StubHolidayService Holidays { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestAuthentication.Configuration));
        builder.ConfigureServices(services =>
        {
            TestAuthentication.Configure(services);
            services.RemoveAll<IPermissionSource>();
            services.AddSingleton<IPermissionSource>(new StubPermissionSource(permissions, unavailable));
            services.RemoveAll<IBankHoliday>();
            services.AddSingleton<IBankHoliday>(Holidays);
        });
    }
}

public sealed class AuthorizationTests
{
    [Theory]
    [InlineData(false, false, 401)]
    [InlineData(true, false, 403)]
    [InlineData(true, true, 200)]
    public async Task DirectHolidayRequestsEnforceBothTokenAndDatabasePermission(bool authenticated, bool permitted, int expected)
    {
        await using var factory = new HolidayFactory(permitted ? [DayNexPermissions.HolidaySync] : []);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (authenticated) client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid()));
        var response = await client.PostAsync("/api/BankHoliday/sync", null);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal(permitted && authenticated ? 1 : 0, factory.Holidays.Calls);
    }

    [Fact]
    public async Task PermissionOutageFailsClosedWithServiceUnavailable()
    {
        await using var factory = new HolidayFactory([], unavailable: true);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid()));
        var response = await client.PostAsync("/api/BankHoliday/sync", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, factory.Holidays.Calls);
    }

    [Fact]
    public async Task SyncRateLimitRejectsExcessRequests()
    {
        await using var factory = new HolidayFactory([DayNexPermissions.HolidaySync]);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/BankHoliday/sync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/BankHoliday/sync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/BankHoliday/sync", null)).StatusCode);
    }
}

public sealed class RecordingHandler(HttpStatusCode status, object? body = null) : HttpMessageHandler
{
    public string? Authorization { get; private set; }
    public string? Path { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Authorization = request.Headers.Authorization?.ToString();
        Path = request.RequestUri?.AbsolutePath;
        return Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(body) });
    }
}

public sealed class PermissionClientTests
{
    [Fact]
    public async Task PermissionLookupForwardsCallerTokenToSelfOnlyEndpoint()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, new UserPermissionsDto(Guid.NewGuid(), [DayNexPermissions.HolidaySync]));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://identity.test/") };
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer caller-access-token";
        var source = new IdentityPermissionClient(client, new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = context });
        var result = await source.GetAsync(new ClaimsPrincipal(), CancellationToken.None);
        Assert.Equal("Bearer caller-access-token", handler.Authorization);
        Assert.Equal("/api/identity/me/permissions", handler.Path);
        Assert.Contains(DayNexPermissions.HolidaySync, result!.Permissions);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task DeniedPermissionLookupNeverGrantsAccess(int status)
    {
        using var client = new HttpClient(new RecordingHandler((HttpStatusCode)status)) { BaseAddress = new Uri("https://identity.test/") };
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer rejected-token";
        var source = new IdentityPermissionClient(client, new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = context });
        Assert.Null(await source.GetAsync(new ClaimsPrincipal(), CancellationToken.None));
    }

    [Fact]
    public async Task ServerFailureBecomesTemporaryPermissionFailure()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("https://identity.test/") };
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";
        var source = new IdentityPermissionClient(client, new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = context });
        await Assert.ThrowsAsync<PermissionServiceUnavailableException>(() => source.GetAsync(new ClaimsPrincipal(), CancellationToken.None));
    }
}
