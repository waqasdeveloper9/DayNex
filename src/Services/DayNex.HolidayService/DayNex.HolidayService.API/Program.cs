using DayNex.Contracts.Identity;
using DayNex.HolidayService.Application;
using DayNex.HolidayService.Infrastructure;
using DayNex.Shared.Http;
using DayNex.Shared.Http.Authentication;
using DayNex.Shared.Http.Authorization;
using DayNex.Shared.Http.Hosting;
using DayNex.Shared.Http.Middleware;

namespace DayNex.HolidayService.API;

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
        builder.Services.AddDayNexPermissions(DayNexPermissions.HolidaySync);
        builder.Services.AddIdentityPermissionClient(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddHttpService();
        var app = builder.Build();
        app.UseForwardedHeaders();

        app.UseDayNexExceptionHandling();
        if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapControllers();
        app.Run();
    }
}
