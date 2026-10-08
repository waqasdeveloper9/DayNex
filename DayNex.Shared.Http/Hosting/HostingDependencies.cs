using System.Threading.RateLimiting;
using DayNex.Shared.Http.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace DayNex.Shared.Http.Hosting;

public static class HostingDependencies
{
    public static IServiceCollection AddDayNexApiDefaults(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TrustedProxyOptions>().Bind(configuration.GetSection("Hosting"))
            .Validate(o => o.KnownProxies.All(p => IPAddress.TryParse(p, out _)), "Hosting:KnownProxies must contain IP addresses.")
            .ValidateOnStart();
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<TrustedProxyOptions>>((options, settings) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (var proxy in settings.Value.KnownProxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
        });
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ExternalIdentity.FromPrincipal(context.User)?.Key ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                    }));
            options.AddPolicy("holiday-sync", context => RateLimitPartition.GetFixedWindowLimiter(
                ExternalIdentity.FromPrincipal(context.User)?.Key ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 2, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellation) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429, title: "Too many requests.").ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }
}

public sealed class TrustedProxyOptions
{
    public string[] KnownProxies { get; set; } = [];
}
