using DayNex.Shared.Http.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace DayNex.Shared.Http;

public static class HTTPDependencies
{
    public static IServiceCollection AddHttpService(this IServiceCollection services)
    {
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan; // The resilience pipeline owns the total timeout.
            client.MaxResponseContentBufferSize = 4 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        }).AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.UseJitter = true;
            options.Retry.DisableForUnsafeHttpMethods();
        });
        return services;
    }
}
