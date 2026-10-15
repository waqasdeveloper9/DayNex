using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;

public sealed record MotLookupResult(string Status, MotVehicleHistory? History = null);
public sealed record MotSummaryDto(string Status, DateTimeOffset? RetrievedUtc, string? HasOutstandingRecall);
public sealed record MotDefectDto(string? Text, string? Type, bool? Dangerous);
public sealed record MotTestDto(
    string? RegistrationAtTimeOfTest, DateTimeOffset? CompletedDate, string TestResult,
    DateOnly? ExpiryDate, string? OdometerValue, string? OdometerUnit,
    string OdometerResultType, string? MotTestNumber, string DataSource, string? Location,
    IReadOnlyList<MotDefectDto> Defects);
public sealed record MotHistoryDto(
    Guid VehicleId, string Status, DateTimeOffset? RetrievedUtc,
    string? Registration, string? Make, string? Model, DateOnly? FirstUsedDate,
    string? FuelType, string? PrimaryColour, DateOnly? RegistrationDate,
    DateOnly? ManufactureDate, string? ManufactureYear, string? EngineSize,
    DateOnly? MotTestDueDate, string? HasOutstandingRecall,
    int Page, int PageSize, int TotalTests, IReadOnlyList<MotTestDto> Tests);
