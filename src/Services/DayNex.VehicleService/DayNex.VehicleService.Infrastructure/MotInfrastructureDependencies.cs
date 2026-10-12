using System.Text.RegularExpressions;
using DayNex.Shared.Http;
using DayNex.VehicleService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DayNex.VehicleService.Infrastructure;

public static partial class MotInfrastructureDependencies
{
    private const string ApiClientName = "Vehicle.Mot";
    private const string TokenClientName = "Vehicle.MotToken";

    public static IServiceCollection AddVehicleMotInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MotOptions>()
            .Bind(configuration.GetSection(MotOptions.Section))
            .Validate(options => !options.Enabled || ValidSettings(options),
                "Enabled MOT requires the DVSA-issued token URL, client ID, client secret, scope and API key.")
            .ValidateOnStart();

        services.AddScoped<IMotHistoryStore, MotHistoryStore>();
        services.AddHttpClient(TokenClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.MaxResponseContentBufferSize = 64 * 1024;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .RemoveAllLoggers();

        services.AddSingleton(provider => new MotAccessTokenProvider(
            CreateApiClient(provider, TokenClientName),
            provider.GetRequiredService<IOptions<MotOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<MotRequestGate>();
        services.AddTransient<MotAuthorizationHandler>();
        services.AddTransient<MotRateLimitHandler>();

        var api = services.AddHttpClient(ApiClientName, (provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<MotOptions>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.MaxResponseContentBufferSize = 4 * 1024 * 1024;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .RemoveAllLoggers();

        // The limiter/auth handlers are inside resilience so every retry is paced and authenticated.
        api.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.UseJitter = true;
        });
        api.AddHttpMessageHandler<MotAuthorizationHandler>();
        api.AddHttpMessageHandler<MotRateLimitHandler>();

        services.AddScoped<IMotHistoryClient>(provider => new MotHistoryClient(
            CreateApiClient(provider, ApiClientName),
            provider.GetRequiredService<IOptions<MotOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        return services;
    }

    private static ApiClient CreateApiClient(IServiceProvider provider, string clientName) => new(
        provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName),
        provider.GetRequiredService<ILogger<ApiClient>>());

    private static bool ValidSettings(MotOptions options) =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var api) &&
        api.Scheme == "https" && api.Host == "history.mot.api.gov.uk" &&
        api.UserInfo.Length == 0 && api.Port == 443 && api.AbsolutePath == "/" &&
        api.Query.Length == 0 && api.Fragment.Length == 0 &&
        options.RegistrationPath == "v1/trade/vehicles/registration/" &&
        Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out var token) &&
        token.Scheme == "https" && token.Host == "login.microsoftonline.com" &&
        token.Port == 443 && token.UserInfo.Length == 0 && token.Query.Length == 0 &&
        token.Fragment.Length == 0 && TokenPath().IsMatch(token.AbsolutePath) &&
        Guid.TryParse(options.ClientId, out _) &&
        options.Scope == "https://tapi.dvsa.gov.uk/.default" &&
        !string.IsNullOrWhiteSpace(options.ClientSecret) && !string.IsNullOrWhiteSpace(options.ApiKey);

    [GeneratedRegex(@"\A/[0-9a-fA-F-]{36}/oauth2/v2\.0/token\z")]
    private static partial Regex TokenPath();
}
