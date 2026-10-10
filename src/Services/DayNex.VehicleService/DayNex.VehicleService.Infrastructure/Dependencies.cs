using System.Net;
using DayNex.Domain.Common.Interface;
using DayNex.Shared.Http;
using DayNex.VehicleService.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

namespace DayNex.VehicleService.Infrastructure;
public static class Dependencies
{
    public static IServiceCollection AddVehicleInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddOptions<VehicleDatabaseOptions>().Bind(config.GetSection("ConnectionStrings")).Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection), "Set the Vehicle database connection using User Secrets or environment variables.").ValidateOnStart();
        services.AddDbContextPool<VehicleDbContext>((sp, db) => db.UseSqlServer(sp.GetRequiredService<IOptions<VehicleDatabaseOptions>>().Value.DefaultConnection, sql => sql.EnableRetryOnFailure(3)));
        services.AddScoped(typeof(IGenericRepository<>), typeof(VehicleRepository<>));
        services.AddScoped<IAssetWriter, AssetWriter>();
        services.AddScoped<CustomerAssetService>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddOptions<DvlaOptions>().Bind(config.GetSection(DvlaOptions.Section)).Validate(o => Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.UserInfo == "" && uri.Host is "driver-vehicle-licensing.api.gov.uk" or "uat.driver-vehicle-licensing.api.gov.uk", "DVLA endpoint must use an approved HTTPS host.").Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.ApiKey), "Set Dvla:ApiKey in secrets before enabling DVLA.").ValidateOnStart();
        services.AddOptions<OutboundProxyOptions>().Bind(config.GetSection("Dvla:Proxy")).Validate(o => !o.Enabled || Uri.TryCreate(o.Address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.UserInfo == "", "Proxy must be an HTTP/HTTPS address without embedded credentials.").ValidateOnStart();
        services.AddOptions<VehicleIdentityOptions>().Bind(config.GetSection("IdentityService")).Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) && uri.UserInfo == "" && (uri.Scheme == "https" || environment.IsDevelopment() && uri.Scheme == "http"), "Identity service must use HTTPS outside Development.").ValidateOnStart();
        services.AddTransient<IdentityBearerHandler>();
        services.AddHttpClient("Vehicle.Identity", (sp, http) =>
        {
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<VehicleIdentityOptions>>().Value.BaseUrl);
            http.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) }).AddHttpMessageHandler<IdentityBearerHandler>().AddStandardResilienceHandler();
        services.AddScoped<ICurrentCustomer>(sp => new CurrentCustomer(new ApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Vehicle.Identity"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiClient>>())));
        services.AddHttpClient("Vehicle.Dvla", (sp, http) =>
        {
            http.Timeout = Timeout.InfiniteTimeSpan;
            http.MaxResponseContentBufferSize = 64 * 1024;
            http.DefaultRequestHeaders.Add("x-api-key", sp.GetRequiredService<IOptions<DvlaOptions>>().Value.ApiKey);
        }).ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var proxy = sp.GetRequiredService<IOptions<OutboundProxyOptions>>().Value;
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                UseProxy = proxy.Enabled,
                Proxy = proxy.Enabled ? new WebProxy(new Uri(proxy.Address))
                {
                    Credentials = string.IsNullOrEmpty(proxy.Username) ? null : new NetworkCredential(proxy.Username, proxy.Password)
                }

                : null
            };
        }).RedactLoggedHeaders(["x-api-key"]).AddStandardResilienceHandler(o =>
        {
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
            o.Retry.MaxRetryAttempts = 2;
            o.Retry.UseJitter = true;
        // This POST is a provider-documented read-only enquiry, so retrying transient responses is safe.
        });
        services.AddScoped<IVehicleLookup>(sp => new DvlaVehicleLookup(new ApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Vehicle.Dvla"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiClient>>()), sp.GetRequiredService<IOptions<DvlaOptions>>(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DvlaVehicleLookup>>()));
        return services;
    }
}
