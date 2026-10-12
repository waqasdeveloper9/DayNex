namespace DayNex.VehicleService.Domain.Models;

public sealed class MotTest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid HistoryId { get; set; }
    public string? RegistrationAtTimeOfTest { get; init; }
    public DateTimeOffset? CompletedDate { get; init; }
    public string TestResult { get; init; } = "";
    public DateOnly? ExpiryDate { get; init; }
    // The provider represents mileage and test numbers as strings, including unavailable values.
    public string? OdometerValue { get; init; }
    public string? OdometerUnit { get; init; }
    public string OdometerResultType { get; init; } = "";
    public string? MotTestNumber { get; init; }
    public string DataSource { get; init; } = "";
    public string? Location { get; init; }
    public List<MotDefect> Defects { get; init; } = [];
}
