using System.Net;
using System.Net.Http.Json;
using DayNex.Shared.Http;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DayNex.Tests;

public sealed class MotClientTests
{
    [Fact]
    public async Task CurrentObjectSchemaMapsIsoDatesAndAllThreeDataSources()
    {
        var handler = new JsonHandler("""
            {"registration":"DA67RVW","make":"TEST","model":"Example","hasOutstandingRecall":"Yes",
             "firstUsedDate":"2017-09-01","engineSize":"1598","motTests":[
              {"completedDate":"2026-01-01T09:17:46.000Z","testResult":"PASSED","expiryDate":"2027-01-01",
               "odometerValue":"001234","odometerUnit":"MI","odometerResultType":"READ",
               "motTestNumber":"000123456789","dataSource":"DVSA","registrationAtTimeOfTest":"DA67RVW",
               "defects":[{"text":"Example advisory","type":"ADVISORY","dangerous":false}]},
              {"completedDate":"2025-01-01T09:17:46Z","testResult":"FAILED","odometerResultType":"NO_ODOMETER",
               "dataSource":"DVA NI","motTestNumber":null},
              {"completedDate":null,"testResult":"PASSED","odometerResultType":"NO_ODOMETER",
               "dataSource":"CVS","location":"Example facility","defects":[{"dangerous":null}]}]}
            """);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://history.mot.api.gov.uk/") };
        var client = Create(http);
        var id = Guid.NewGuid();
        var result = await client.FetchAsync(id, "DA67RVW", default);

        Assert.Equal("Available", result.Status);
        Assert.Equal(id, result.History!.Id);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://history.mot.api.gov.uk/v1/trade/vehicles/registration/DA67RVW", handler.Uri);
        Assert.Equal(new DateOnly(2017, 9, 1), result.History.FirstUsedDate);
        Assert.Equal(3, result.History.Tests.Count);
        Assert.Equal("000123456789", result.History.Tests[0].MotTestNumber);
        Assert.Equal("001234", result.History.Tests[0].OdometerValue);
        Assert.False(result.History.Tests[0].Defects[0].Dangerous);
        Assert.Null(result.History.Tests[2].CompletedDate);
        Assert.Null(result.History.Tests[2].Defects[0].Dangerous);
    }

    [Fact]
    public async Task NewlyRegisteredVehicleCanHaveNoMotTests()
    {
        using var http = new HttpClient(new JsonHandler("""
            {"registration":"DA67RVW","manufactureYear":"2026","motTestDueDate":"2029-09-01","hasOutstandingRecall":"Unknown"}
            """)) { BaseAddress = new Uri("https://history.mot.api.gov.uk/") };
        var result = await Create(http).FetchAsync(Guid.NewGuid(), "DA67RVW", default);
        Assert.Equal("Available", result.Status);
        Assert.Empty(result.History!.Tests);
        Assert.Equal(new DateOnly(2029, 9, 1), result.History.MotTestDueDate);
    }

    [Theory]
    [InlineData(401, "ProviderAccessDenied")]
    [InlineData(403, "ProviderAccessDenied")]
    [InlineData(404, "NotFound")]
    [InlineData(429, "RateLimited")]
    [InlineData(503, "Unavailable")]
    public async Task ProviderErrorsBecomeIndependentSafeStatuses(int status, string expected)
    {
        using var http = new HttpClient(new JsonHandler("{}", (HttpStatusCode)status))
        { BaseAddress = new Uri("https://history.mot.api.gov.uk/") };
        Assert.Equal(expected, (await Create(http).FetchAsync(Guid.NewGuid(), "DA67RVW", default)).Status);
    }

    [Theory]
    [InlineData("""[{"registration":"DA67RVW"}]""")]
    [InlineData("""{"registration":"AA11AAA","hasOutstandingRecall":"No"}""")]
    [InlineData("""{"registration":"DA67RVW","hasOutstandingRecall":"No","motTests":[{}]}""")]
    public async Task DeprecatedMismatchedAndInvalidPayloadsCannotOverwriteHistory(string json)
    {
        using var http = new HttpClient(new JsonHandler(json))
        { BaseAddress = new Uri("https://history.mot.api.gov.uk/") };
        Assert.Equal("InvalidResponse", (await Create(http).FetchAsync(Guid.NewGuid(), "DA67RVW", default)).Status);
    }

    [Fact]
    public async Task DisabledClientDoesNotCallDvsa()
    {
        var handler = new JsonHandler("{}");
        using var http = new HttpClient(handler);
        var client = new MotHistoryClient(new ApiClient(http, NullLogger<ApiClient>.Instance),
            Options.Create(new MotOptions()), TimeProvider.System);
        Assert.Equal("NotConfigured", (await client.FetchAsync(Guid.NewGuid(), "DA67RVW", default)).Status);
        Assert.Equal(0, handler.Calls);
    }

    private static MotHistoryClient Create(HttpClient http) => new(
        new ApiClient(http, NullLogger<ApiClient>.Instance),
        Options.Create(new MotOptions { Enabled = true, RegistrationPath = "v1/trade/vehicles/registration/" }),
        TimeProvider.System);

    private sealed class JsonHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Uri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Method = request.Method;
            Uri = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(status)
            { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
