using DayNex.UserInfoService.API.Endpoints;
using DayNex.Shared.Http.Authentication;
using DayNex.Shared.Http.Hosting;
using DayNex.Shared.Http.Middleware;
using DayNex.UserInfoService.Infrastructure;
using DayNex.UserInfoService.Application;

namespace DayNex.UserInfoService.API;
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddDayNexAuthentication(builder.Configuration);
        builder.Services.AddDayNexApiDefaults(builder.Configuration);
        builder.Services.AddUserInfoApplication();
        builder.Services.AddVehicleRefreshEndpoint(builder.Configuration);
        builder.Services.AddUserInfoInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.AddRateLimiter(o => o.AddPolicy("vehicle-lookup", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ExternalIdentity.FromPrincipal(context.User)?.Key ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
        builder.Services.AddRateLimiter(o => o.AddPolicy("userinfo-lookup", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ExternalIdentity.FromPrincipal(context.User)?.Key ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseDayNexExceptionHandling();
        if (app.Environment.IsDevelopment())
            app.MapOpenApi().AllowAnonymous();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapControllers();
        app.MapVehicleRefreshEndpoint();
        app.Run();
    }
}
