using DayNex.Domain.Common.Interface;
using System.Text.RegularExpressions;
namespace DayNex.UserInfoService.Domain.Models;
public sealed partial class UserLocation : IEntity
{
    public Guid Id { get; set; } // Logical Identity user ID; no cross-service foreign key.
    public string Postcode { get; set; } = "";
    public Guid Revision { get; set; } = Guid.NewGuid();
    public string LookupStatus { get; set; } = "Pending";
    public long? LastAttemptUtcTicks { get; set; }
    public DateTimeOffset? RetrievedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public UserCouncil? Council { get; set; }
    public static string Normalize(string? postcode)
    {
        var value = (postcode ?? "").Trim().ToUpperInvariant();
        if (value.Length > 10 || value.Length > 0 && !PostcodePattern().IsMatch(value))
            throw new ArgumentException("Enter a valid UK postcode or leave it empty.");
        value = string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
        return value.Length == 0 ? "" : value.Insert(value.Length - 3, " ");
    }
    [GeneratedRegex(@"\A(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}|GIR\s*0AA)\z", RegexOptions.CultureInvariant)]
    private static partial Regex PostcodePattern();
}
public sealed class UserCouncil
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string HomepageUrl { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Slug { get; set; } = "";
    public string RawResponseJson { get; set; } = "{}";
}
