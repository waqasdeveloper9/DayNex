using DayNex.Shared.Http.Authentication;
using DayNex.Shared.Http.Hosting;
using DayNex.Shared.Http.Middleware;
using DayNex.VehicleService.Infrastructure;
using DayNex.VehicleService.Application;

namespace DayNex.VehicleService.API;
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
        builder.Services.AddVehicleApplication();
        builder.Services.AddVehicleInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.AddRateLimiter(o => o.AddPolicy("vehicle-lookup", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ExternalIdentity.FromPrincipal(context.User)?.Key ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
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
        app.Run();
    }
}
