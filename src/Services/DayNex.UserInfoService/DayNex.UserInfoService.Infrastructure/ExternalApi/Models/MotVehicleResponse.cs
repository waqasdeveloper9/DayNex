namespace DayNex.UserInfoService.Infrastructure.ExternalApi.Models;

// Current /v1 response: one object, ISO dates, defects (not the deprecated array/rfrAndComments shape).
public sealed class MotVehicleResponse
{
    public string? Registration { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public DateOnly? FirstUsedDate { get; init; }
    public string? FuelType { get; init; }
    public string? PrimaryColour { get; init; }
    public DateOnly? RegistrationDate { get; init; }
    public DateOnly? ManufactureDate { get; init; }
    public string? ManufactureYear { get; init; }
    public string? EngineSize { get; init; }
    public DateOnly? MotTestDueDate { get; init; }
    public string? HasOutstandingRecall { get; init; }
    public List<MotTestResponse>? MotTests { get; init; }
}

