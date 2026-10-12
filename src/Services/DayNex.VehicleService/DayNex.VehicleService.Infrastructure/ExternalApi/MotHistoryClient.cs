using System.Net;
using System.Text.Json;
using DayNex.Shared.Http.Interface;
using DayNex.VehicleService.Application;
using DayNex.VehicleService.Domain.Models;
using DayNex.VehicleService.Infrastructure.ExternalApi.Models;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace DayNex.VehicleService.Infrastructure;

public sealed class MotHistoryClient(
    IApiClient apiClient, IOptions<MotOptions> options, TimeProvider clock) : IMotHistoryClient
{
    public async Task<MotLookupResult> FetchAsync(
        Guid vehicleId, string registration, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return new("NotConfigured");
        try
        {
            var endpoint = options.Value.RegistrationPath + Uri.EscapeDataString(registration);
            var response = await apiClient.GetAsync<MotVehicleResponse>(endpoint, cancellationToken);
            if (response is null ||
                UserVehicle.Normalize(response.Registration) != registration ||
                response.HasOutstandingRecall is not ("Yes" or "No" or "Unknown" or "Unavailable") ||
                response.MotTests is { Count: > 5000 } ||
                response.MotTests?.Any(test => !ValidTest(test)) == true)
                return new("InvalidResponse");

            return new("Available", new MotVehicleHistory(vehicleId)
            {
                Registration = registration,
                Make = response.Make, Model = response.Model,
                FirstUsedDate = response.FirstUsedDate, FuelType = response.FuelType,
                PrimaryColour = response.PrimaryColour, RegistrationDate = response.RegistrationDate,
                ManufactureDate = response.ManufactureDate, ManufactureYear = response.ManufactureYear,
                EngineSize = response.EngineSize, MotTestDueDate = response.MotTestDueDate,
                HasOutstandingRecall = response.HasOutstandingRecall,
                RetrievedUtc = clock.GetUtcNow(),
                Tests = (response.MotTests ?? []).Select(test => new MotTest
                {
                    HistoryId = vehicleId,
                    RegistrationAtTimeOfTest = test.RegistrationAtTimeOfTest,
                    CompletedDate = test.CompletedDate, TestResult = test.TestResult!,
                    ExpiryDate = test.ExpiryDate, OdometerValue = test.OdometerValue,
                    OdometerUnit = test.OdometerUnit, OdometerResultType = test.OdometerResultType!,
                    MotTestNumber = test.MotTestNumber, DataSource = test.DataSource!,
                    Location = test.Location,
                    Defects = (test.Defects ?? []).Select(defect => new MotDefect
                    {
                        Text = defect.Text, Type = defect.Type, Dangerous = defect.Dangerous
                    }).ToList()
                }).ToList()
            });
        }
        catch (MotAuthenticationException) { return new("ProviderAccessDenied"); }
        catch (HttpRequestException exception)
        {
            return new(exception.StatusCode switch
            {
                HttpStatusCode.NotFound => "NotFound",
                HttpStatusCode.BadRequest => "Rejected",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ProviderAccessDenied",
                HttpStatusCode.TooManyRequests => "RateLimited",
                _ => "Unavailable"
            });
        }
        catch (Exception exception) when (exception is TimeoutRejectedException or BrokenCircuitException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        { return new("Unavailable"); }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        { return new("InvalidResponse"); }
    }

    private static bool ValidTest(MotTestResponse test) =>
        test.TestResult is "PASSED" or "FAILED" &&
        test.OdometerResultType is "READ" or "UNREADABLE" or "NO_ODOMETER" &&
        test.DataSource is "DVSA" or "DVA NI" or "CVS" &&
        (test.CompletedDate is not null || test.DataSource == "CVS") &&
        test.Defects is not { Count: > 1000 };
}
