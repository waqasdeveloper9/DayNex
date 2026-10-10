namespace DayNex.VehicleService.Domain;
// Provider schema: optional properties remain nullable; absence is not false or zero.
public sealed record VehicleDetails
{
    public string RegistrationNumber { get; init; } = "";
    public string? TaxStatus { get; init; }
    public DateOnly? TaxDueDate { get; init; }
    public DateOnly? ArtEndDate { get; init; }
    public string? MotStatus { get; init; }
    public DateOnly? MotExpiryDate { get; init; }
    public string? Make { get; init; }
    public string? MonthOfFirstDvlaRegistration { get; init; }
    public string? MonthOfFirstRegistration { get; init; }
    public int? YearOfManufacture { get; init; }
    public int? EngineCapacity { get; init; }
    public int? Co2Emissions { get; init; }
    public string? FuelType { get; init; }
    public bool? MarkedForExport { get; init; }
    public string? Colour { get; init; }
    public string? TypeApproval { get; init; }
    public string? Wheelplan { get; init; }
    public int? RevenueWeight { get; init; }
    public string? RealDrivingEmissions { get; init; }
    public DateOnly? DateOfLastV5CIssued { get; init; }
    public string? EuroStatus { get; init; }
    public bool? AutomatedVehicle { get; init; }
}
