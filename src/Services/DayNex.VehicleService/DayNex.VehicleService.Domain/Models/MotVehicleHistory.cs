using DayNex.Domain.Common.Interface;

namespace DayNex.VehicleService.Domain.Models;

/// <summary>The latest successful DVSA snapshot for one saved user vehicle.</summary>
public sealed class MotVehicleHistory : IEntity
{
    private MotVehicleHistory() { }

    public MotVehicleHistory(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty) throw new ArgumentException("A vehicle is required.", nameof(vehicleId));
        Id = vehicleId;
    }

    public Guid Id { get; private set; }
    public string Registration { get; init; } = "";
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
    public string HasOutstandingRecall { get; init; } = "Unknown";
    public DateTimeOffset RetrievedUtc { get; init; }
    public List<MotTest> Tests { get; init; } = [];
}
