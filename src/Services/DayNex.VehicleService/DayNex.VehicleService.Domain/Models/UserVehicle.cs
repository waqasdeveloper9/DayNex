using DayNex.Domain.Common.Interface;
using System.Text.RegularExpressions;

namespace DayNex.VehicleService.Domain.Models;
public sealed partial class UserVehicle : IEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RegistrationNumber { get; private set; } = "";
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset? LastAttemptUtc { get; private set; }
    public DateTimeOffset? RetrievedUtc { get; private set; }
    public string LookupStatus { get; private set; } = "Pending";
    public VehicleDetails? Details { get; private set; }

    private UserVehicle()
    {
    }

    public static UserVehicle Create(Guid userId, string registration, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A user is required.");
        return new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegistrationNumber = Normalize(registration),
            CreatedUtc = now
        };
    }

    public static string Normalize(string? value)
    {
        var registration = (value ?? "").Trim().ToUpperInvariant().Replace(" ", "");
        if (!RegistrationPattern().IsMatch(registration))
            throw new ArgumentException("Enter 2–7 letters or numbers for the vehicle registration.");
        return registration;
    }

    public void RecordAttempt(string status, DateTimeOffset now)
    {
        LookupStatus = status;
        LastAttemptUtc = now;
    }

    public void Apply(VehicleDetails details, DateTimeOffset now)
    {
        if (Normalize(details.RegistrationNumber) != RegistrationNumber)
            throw new InvalidOperationException("Provider returned a different vehicle.");
        Details = details;
        RetrievedUtc = now;
        RecordAttempt("Available", now);
    }

    [GeneratedRegex(@"\A[A-Z0-9]{2,7}\z", RegexOptions.CultureInvariant)]
    private static partial Regex RegistrationPattern();
}
