using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Domain.Enums;

namespace DayNex.IdentityService.Domain.Entities;

public sealed class User : IEntity
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private User() { }

    public static User CreateFromExternalIdentity(string externalId, string? email, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External id is required.", nameof(externalId));
        if (externalId.Length > 100)
            throw new ArgumentException("External identity is too long.", nameof(externalId));

        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Email = Clean(email, 320),
            DisplayName = Clean(displayName, 200) ?? "DayNex user",
            Role = UserRole.User,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public void ChangeRole(UserRole newRole) => Role = newRole;

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}
