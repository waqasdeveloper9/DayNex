namespace DayNex.VehicleService.Infrastructure.ExternalApi.Models;

public sealed class MotTestResponse
{
    public string? RegistrationAtTimeOfTest { get; init; }
    public DateTimeOffset? CompletedDate { get; init; }
    public string? TestResult { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? OdometerValue { get; init; }
    public string? OdometerUnit { get; init; }
    public string? OdometerResultType { get; init; }
    public string? MotTestNumber { get; init; }
    public string? DataSource { get; init; }
    public string? Location { get; init; }
    public List<MotDefectResponse>? Defects { get; init; }
}

