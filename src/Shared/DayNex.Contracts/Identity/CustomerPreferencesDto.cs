namespace DayNex.Contracts.Identity;
public sealed record CustomerPreferencesDto(
    string? VehicleRegistration = null, string? CompanyNumber = null, string Postcode = "",
    string HolidayRegion = "EnglandAndWales", bool OnboardingCompleted = false);
