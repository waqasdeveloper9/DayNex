using DayNex.Shared.Http.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
using Polly.CircuitBreaker;

namespace DayNex.Shared.Http.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected. Do not try to write to a closed response.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request failed at {Path}; trace {TraceId}", context.Request.Path, context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            var (status, title) = ex switch
            {
                PermissionServiceUnavailableException => (503, "The permission service is temporarily unavailable."),
                HttpRequestException => (502, "An upstream service failed."),
                TimeoutRejectedException or OperationCanceledException => (504, "An upstream service timed out."),
                BrokenCircuitException => (503, "An upstream service is temporarily unavailable."),
                _ => (500, "An unexpected error occurred.")
            };
            await Results.Problem(statusCode: status, title: title,
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseDayNexExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
