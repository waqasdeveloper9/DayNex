using DayNex.Shared.Http.Authentication;
using DayNex.Shared.Http.Hosting;
using DayNex.Shared.Http.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

namespace DayNex.Gateway;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();
            builder.Services.AddSerilog((services, logger) => logger.ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services).WriteTo.Console());
            builder.Services.AddDayNexAuthentication(builder.Configuration);
            builder.Services.AddDayNexApiDefaults(builder.Configuration);
            builder.Services.AddOcelot(builder.Configuration);
            var app = builder.Build();
            app.UseForwardedHeaders();
            app.UseDayNexExceptionHandling();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseRateLimiter();
            // Ocelot routes are middleware, not MVC endpoints. Explicitly enforce our scope policy here.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/health")
                {
                    await Microsoft.AspNetCore.Http.Results.Ok(new { status = "Healthy" }).ExecuteAsync(context);
                    return;
                }
                var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
                var result = await authorization.AuthorizeAsync(context.User, null, AuthenticationDependencies.ApiAccess);
                if (!result.Succeeded)
                {
                    if (context.User.Identity?.IsAuthenticated == true) await context.ForbidAsync();
                    else await context.ChallengeAsync();
                    return;
                }
                await next();
            });
            await app.UseOcelot();
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Gateway terminated unexpectedly");
            throw;
        }
        finally { await Log.CloseAndFlushAsync(); }
    }
}
