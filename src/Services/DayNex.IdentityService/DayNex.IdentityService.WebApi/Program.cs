using DayNex.IdentityService.Infrastructure;
using DayNex.IdentityService.Application;
using DayNex.Shared.Http.Authentication;
using DayNex.Shared.Http.Hosting;
using DayNex.Shared.Http.Middleware;

namespace DayNex.IdentityService.WebApi;

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
        builder.Services.AddIdentityInfrastructure(builder.Configuration);
        builder.Services.AddIdentityApplication();
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
