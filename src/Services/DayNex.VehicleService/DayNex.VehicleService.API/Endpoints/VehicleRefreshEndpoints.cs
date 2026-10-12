using DayNex.VehicleService.API.Authentication;
using DayNex.VehicleService.API.Configuration;
using DayNex.VehicleService.Application;
using Microsoft.Extensions.Options;
namespace DayNex.VehicleService.API.Endpoints;

public static class VehicleRefreshEndpoints
{
    public static IServiceCollection AddVehicleRefreshEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VehicleRefreshOptions>().Bind(configuration.GetSection(VehicleRefreshOptions.Section))
            .Validate(o => o.BatchSize is >= 1 and <= 3, "Vehicle refresh batch size must be 1–3.")
            .Validate(o => !o.Enabled || o.ApiKey.Length is >= 32 and <= 256, "Enabled refresh requires a separate secret of at least 32 characters.")
            .Validate(o => System.Text.RegularExpressions.Regex.IsMatch(o.Endpoint, @"\Ainternal/jobs/[a-z0-9]+(?:/[a-z0-9-]+)*\z"),
                "Job endpoint must be a relative path below internal/jobs/.")
            .ValidateOnStart();
        services.AddAuthentication().AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
            RefreshJobAuthenticationHandler>(RefreshJobAuthenticationHandler.SchemeName, _ => { });
        services.AddAuthorization(o => o.AddPolicy("VehicleRefreshJobOnly", p =>
            p.AddAuthenticationSchemes(RefreshJobAuthenticationHandler.SchemeName).RequireAuthenticatedUser()));
        return services;
    }

    public static void MapVehicleRefreshEndpoint(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<VehicleRefreshOptions>>().Value;
        app.MapPost("/" + settings.Endpoint, async (VehicleRefreshJob job, HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromMinutes(4));
            try { return Results.Ok(await job.RunBatchAsync(settings.BatchSize, deadline.Token)); }
            catch (RefreshJobBusyException)
            {
                context.Response.Headers.RetryAfter = "60";
                return Results.Problem(statusCode: 409, title: "A vehicle refresh batch is already running.");
            }
        }).RequireAuthorization("VehicleRefreshJobOnly");
    }
}

