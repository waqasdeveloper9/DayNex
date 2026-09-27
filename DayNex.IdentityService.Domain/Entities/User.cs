using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Domain.Enums;

namespace DayNex.IdentityService.Domain.Entities;

public sealed class User : IEntity
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private User() { }

    public static User CreateFromExternalIdentity(string externalId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External id is required.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            Role = UserRole.User,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public void ChangeRole(UserRole newRole) => Role = newRole;
}