using DayNex.Domain.Common.Interface;
using System.Text.RegularExpressions;

namespace DayNex.VehicleService.Domain;
public sealed partial class UserCompany : IEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CompanyNumber { get; private set; } = "";
    public DateTimeOffset CreatedUtc { get; private set; }

    private UserCompany()
    {
    }

    public static UserCompany Create(Guid userId, string number, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A user is required.");
        var normalized = (number ?? "").Trim().ToUpperInvariant();
        if (!CompanyPattern().IsMatch(normalized))
            throw new ArgumentException("Enter an 8-character company number.");
        return new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyNumber = normalized,
            CreatedUtc = now
        };
    }

    [GeneratedRegex(@"\A[A-Z0-9]{8}\z", RegexOptions.CultureInvariant)]
    private static partial Regex CompanyPattern();
}
