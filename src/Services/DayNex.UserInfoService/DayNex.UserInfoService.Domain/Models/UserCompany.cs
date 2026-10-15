using DayNex.Domain.Common.Interface;
using System.Text.RegularExpressions;
namespace DayNex.UserInfoService.Domain.Models;
public sealed partial class UserCompany : IEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CompanyNumber { get; private set; } = "";
    public DateTimeOffset CreatedUtc { get; private set; }
    public string LookupStatus { get; private set; } = "Pending";
    public DateTimeOffset? LastAttemptUtc { get; private set; }
    public DateTimeOffset? RetrievedUtc { get; private set; }
    public CompanyProfile? Profile { get; private set; }
    private UserCompany() { }
    public static string Normalize(string? number)
    {
        var normalized = (number ?? "").Trim().ToUpperInvariant();
        if (!CompanyPattern().IsMatch(normalized)) throw new ArgumentException("Enter an 8-character company number, including leading zeros.");
        return normalized;
    }
    public static UserCompany Create(Guid userId, string number, DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.");
        return new() { Id = Guid.NewGuid(), UserId = userId, CompanyNumber = Normalize(number), CreatedUtc = now };
    }
    public void RecordLookup(string status, DateTimeOffset attempt, DateTimeOffset? retrieved)
    {
        LookupStatus = status; LastAttemptUtc = attempt;
        if (retrieved.HasValue) RetrievedUtc = retrieved;
    }
    [GeneratedRegex(@"\A[A-Z0-9]{8}\z", RegexOptions.CultureInvariant)]
    private static partial Regex CompanyPattern();
}
