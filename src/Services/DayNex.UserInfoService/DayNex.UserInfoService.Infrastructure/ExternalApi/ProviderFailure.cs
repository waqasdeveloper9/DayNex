using System.Net;
using System.Text.Json;
using Polly.Timeout;
using Polly.CircuitBreaker;
namespace DayNex.UserInfoService.Infrastructure;
internal static class ProviderFailure
{
    public static bool Handles(Exception error,CancellationToken token) => error is HttpRequestException or JsonException or ArgumentException or InvalidOperationException or TimeoutRejectedException or BrokenCircuitException || error is OperationCanceledException && !token.IsCancellationRequested;
    public static string Status(Exception error) => error switch {
        HttpRequestException http => http.StatusCode switch {
            HttpStatusCode.NotFound => "NotFound",
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "Rejected",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ProviderAccessDenied",
            HttpStatusCode.TooManyRequests => "RateLimited",
            _ => "Unavailable"
        },
        JsonException or ArgumentException or InvalidOperationException => "InvalidResponse",
        _ => "Unavailable"
    };
}
