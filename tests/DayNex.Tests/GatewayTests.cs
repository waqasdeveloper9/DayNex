using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Requester;
using Xunit;

namespace DayNex.Tests;

public sealed class GatewayFactory : WebApplicationFactory<DayNex.Gateway.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestAuthentication.Configuration));
        builder.ConfigureServices(services =>
        {
            TestAuthentication.Configure(services);
            services.AddTransient<GatewayProbeHandler>();
            services.AddTransient(provider => new GlobalDelegatingHandler(provider.GetRequiredService<GatewayProbeHandler>()));
        });
    }
}

public sealed class GatewayProbeHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
            {
                path = request.RequestUri!.AbsolutePath,
                authorization = request.Headers.Authorization?.ToString()
            }), System.Text.Encoding.UTF8, "application/json")
        });
}

public sealed class GatewayTests
{
    [Fact]
    public async Task GatewayHealthIsAvailableWithoutAuthentication()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
    [Fact]
    public async Task GatewayRejectsMissingTokensAndWrongScopesBeforeForwarding()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestAuthentication.Token(Guid.NewGuid(), "wrong_scope"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/identity/me")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/identity/me", "/api/identity/me")]
    [InlineData("GET", "/api/identity/me/permissions", "/api/identity/me/permissions")]
    [InlineData("GET", "/api/identity/me/preferences", "/api/identity/me/preferences")]
    [InlineData("PUT", "/api/identity/me/preferences", "/api/identity/me/preferences")]
    [InlineData("GET", "/api/holidays/upcoming", "/api/holidays/upcoming")]
    [InlineData("POST", "/api/holidays/sync", "/api/BankHoliday/sync")]
    public async Task ValidTokenIsForwardedUnchangedToTheConfiguredService(string method, string upstream, string downstream)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var token = TestAuthentication.Token(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), upstream));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(downstream, document.RootElement.GetProperty("path").GetString());
        Assert.Equal("Bearer " + token, document.RootElement.GetProperty("authorization").GetString());
    }
}
