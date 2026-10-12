using System.Net;
using DayNex.Domain.Common.Interface;
using DayNex.Shared.Http;
using DayNex.VehicleService.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DayNex.VehicleService.Infrastructure;

public static class InfrastructureDependencies
{
    private const string IdentityClientName = "Vehicle.Identity";
    private const string DvlaClientName = "Vehicle.Dvla";

    public static IServiceCollection AddVehicleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ConfigureOptions(services, configuration, environment);

        services.AddDbContextPool<VehicleDbContext>((provider, options) =>
            options.UseSqlServer(
                provider.GetRequiredService<IOptions<VehicleDatabaseOptions>>().Value.DefaultConnection,
                sql => sql.EnableRetryOnFailure(3)), poolSize: 128);

        services.AddScoped(typeof(IGenericRepository<>), typeof(VehicleRepository<>));
        services.AddScoped<IAssetWriter, AssetWriter>();
        services.AddScoped<IVehicleRefreshJobStore, VehicleRefreshJobStore>();
        services.AddHttpContextAccessor();

        AddIdentityClient(services);
        AddDvlaClient(services);
        services.AddVehicleMotInfrastructure(configuration);
        return services;
    }

    private static void ConfigureOptions(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<VehicleDatabaseOptions>()
            .Bind(configuration.GetSection("ConnectionStrings"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultConnection),
                "Configure ConnectionStrings:DefaultConnection using secrets or environment variables.")
            .ValidateOnStart();

        services.AddOptions<DvlaOptions>()
            .Bind(configuration.GetSection(DvlaOptions.Section))
            .Validate(options => IsDvlaEndpoint(options.Endpoint),
                "DVLA endpoint must use an approved HTTPS host.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ApiKey),
                "Set Dvla:ApiKey in secrets before enabling DVLA.")
            .ValidateOnStart();

        services.AddOptions<OutboundProxyOptions>()
            .Bind(configuration.GetSection("Dvla:Proxy"))
            .Validate(options => !options.Enabled ||
                Uri.TryCreate(options.Address, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0,
                "Proxy must be an HTTP/HTTPS address without embedded credentials.")
            .ValidateOnStart();

        services.AddOptions<VehicleIdentityOptions>()
            .Bind(configuration.GetSection("IdentityService"))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                uri.UserInfo.Length == 0 &&
                (uri.Scheme == "https" || environment.IsDevelopment() && uri.Scheme == "http"),
                "Identity service must use HTTPS outside Development.")
            .Validate(options => IsRelativeEndpoint(options.ProfileEndpoint),
                "IdentityService:ProfileEndpoint must be a relative API path.")
            .ValidateOnStart();
    }

    private static void AddIdentityClient(IServiceCollection services)
    {
        services.AddTransient<IdentityBearerHandler>();
        services.AddHttpClient(IdentityClientName, (provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<VehicleIdentityOptions>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddHttpMessageHandler<IdentityBearerHandler>()
            .AddStandardResilienceHandler();

        services.AddScoped<ICurrentCustomer>(provider => new CurrentCustomer(
            CreateApiClient(provider, IdentityClientName),
            provider.GetRequiredService<IOptions<VehicleIdentityOptions>>()));
    }

    private static void AddDvlaClient(IServiceCollection services)
    {
        services.AddHttpClient(DvlaClientName, (provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<DvlaOptions>>().Value;
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.MaxResponseContentBufferSize = 64 * 1024;
                client.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);
            })
            .ConfigurePrimaryHttpMessageHandler(provider => CreateDvlaHandler(
                provider.GetRequiredService<IOptions<OutboundProxyOptions>>().Value))
            .RedactLoggedHeaders(["x-api-key"])
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
                // DVLA documents this POST as a read-only enquiry; transient retries are safe.
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.UseJitter = true;
            });

        services.AddScoped<IVehicleLookup>(provider => new DvlaVehicleLookup(
            CreateApiClient(provider, DvlaClientName),
            provider.GetRequiredService<IOptions<DvlaOptions>>(),
            provider.GetRequiredService<ILogger<DvlaVehicleLookup>>()));
    }

    private static ApiClient CreateApiClient(IServiceProvider provider, string clientName) => new(
        provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName),
        provider.GetRequiredService<ILogger<ApiClient>>());

    private static SocketsHttpHandler CreateDvlaHandler(OutboundProxyOptions settings)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            UseProxy = settings.Enabled
        };

        if (settings.Enabled)
        {
            handler.Proxy = new WebProxy(new Uri(settings.Address))
            {
                Credentials = string.IsNullOrEmpty(settings.Username)
                    ? null
                    : new NetworkCredential(settings.Username, settings.Password)
            };
        }

        return handler;
    }

    private static bool IsDvlaEndpoint(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" && uri.UserInfo.Length == 0 &&
        uri.Host is "driver-vehicle-licensing.api.gov.uk" or "uat.driver-vehicle-licensing.api.gov.uk";

    private static bool IsRelativeEndpoint(string endpoint) =>
        !string.IsNullOrWhiteSpace(endpoint) &&
        Uri.TryCreate(endpoint, UriKind.Relative, out _) &&
        !endpoint.StartsWith('/') && !endpoint.Contains(':') &&
        !endpoint.Contains("..") && !endpoint.Contains('\\');
}