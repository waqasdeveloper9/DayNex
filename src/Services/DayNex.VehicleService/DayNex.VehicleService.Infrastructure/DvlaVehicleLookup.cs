using System.Net;
using System.Text.Json;
using DayNex.Shared.Http;
using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
using Polly.CircuitBreaker;

namespace DayNex.VehicleService.Infrastructure;
public sealed record DvlaVehicleRequest(string RegistrationNumber);
public sealed record DvlaErrorResponse(DvlaError[] Errors);
public sealed record DvlaError(string? Status, string? Code, string Title, string? Detail);
public sealed class DvlaVehicleLookup(ApiClient client, IOptions<DvlaOptions> options, ILogger<DvlaVehicleLookup> logger) : IVehicleLookup
{
    public async Task<VehicleLookupResult> LookupAsync(string registration, CancellationToken token)
    {
        if (!options.Value.Enabled)
            return new("NotConfigured");
        try
        {
            var details = await client.PostAsync<VehicleDetails>(options.Value.Endpoint, new DvlaVehicleRequest(registration), token);
            if (details is null || UserVehicle.Normalize(details.RegistrationNumber) != registration)
                return new("InvalidResponse");
            return new("Available", details);
        }
        catch (HttpRequestException ex)
        {
            // Never log registration, request/response bodies, credentials or provider error details.
            logger.LogWarning("DVLA lookup failed with HTTP status {StatusCode}", (int? )ex.StatusCode);
            return new(ex.StatusCode switch
            {
                HttpStatusCode.NotFound => "NotFound",
                HttpStatusCode.BadRequest => "Rejected",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ProviderAccessDenied",
                HttpStatusCode.TooManyRequests => "RateLimited",
                _ => "Unavailable"
            });
        }
        catch (Exception ex)when (ex is TimeoutRejectedException or BrokenCircuitException || ex is OperationCanceledException && !token.IsCancellationRequested)
        {
            return new("Unavailable");
        }
        catch (Exception ex)when (ex is JsonException or ArgumentException)
        {
            return new("InvalidResponse");
        }
    }
}
