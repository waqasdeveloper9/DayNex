using System.Text.RegularExpressions;
namespace DayNex.IdentityService.Domain.Entities;
public sealed partial class CustomerPreferences
{
    public Guid UserId { get; private set; }
    public string? VehicleRegistration { get; private set; }
    public string? CompanyNumber { get; private set; }
    public string Postcode { get; private set; } = "";
    public string HolidayRegion { get; private set; } = "EnglandAndWales";
    public DateTime UpdatedUtc { get; private set; }
    private CustomerPreferences() { }
    public static CustomerPreferences Create(Guid userId, string? vehicle, string? company, string? postcode, string? region, DateTime now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A customer is required.");
        var normalizedPostcode = (postcode ?? "").Trim().ToUpperInvariant();
        if (normalizedPostcode.Length > 10 || !PostcodePattern().IsMatch(normalizedPostcode))
            throw new ArgumentException("Enter a valid UK postcode.");
        var normalizedVehicle = string.IsNullOrWhiteSpace(vehicle) ? null : vehicle.Trim().ToUpperInvariant();
        if (normalizedVehicle is not null && (normalizedVehicle.Length > 12 || !VehiclePattern().IsMatch(normalizedVehicle)))
            throw new ArgumentException("Vehicle registration must contain 2–12 letters, numbers or spaces.");
        var normalizedCompany = string.IsNullOrWhiteSpace(company) ? null : company.Trim().ToUpperInvariant();
        if (normalizedCompany is not null && !CompanyPattern().IsMatch(normalizedCompany))
            throw new ArgumentException("Company number must contain exactly 8 letters or numbers.");
        if (region is not ("EnglandAndWales" or "Scotland" or "NorthernIreland"))
            throw new ArgumentException("Choose a supported holiday region.");
        normalizedPostcode = string.Concat(normalizedPostcode.Where(c => !char.IsWhiteSpace(c)));
        return new() {
            UserId = userId, VehicleRegistration = normalizedVehicle?.Replace(" ", ""),
            CompanyNumber = normalizedCompany,
            Postcode = normalizedPostcode.Insert(normalizedPostcode.Length - 3, " "),
            HolidayRegion = region, UpdatedUtc = now
        };
    }
    [GeneratedRegex(@"\A(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}|GIR\s*0AA)\z", RegexOptions.CultureInvariant)]
    private static partial Regex PostcodePattern();
    [GeneratedRegex(@"\A[A-Z0-9 ]{2,12}\z", RegexOptions.CultureInvariant)]
    private static partial Regex VehiclePattern();
    [GeneratedRegex(@"\A[A-Z0-9]{8}\z", RegexOptions.CultureInvariant)]
    private static partial Regex CompanyPattern();
}
