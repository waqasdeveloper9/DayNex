namespace DayNex.Contracts.Identity;
public sealed record CustomerPreferencesDto(
    string Postcode = "", string HolidayRegion = "EnglandAndWales", bool OnboardingCompleted = false);