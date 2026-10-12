namespace DayNex.VehicleService.Domain.Models;

public sealed class MotDefect
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public string? Text { get; init; }
    public string? Type { get; init; }
    public bool? Dangerous { get; init; }
}
