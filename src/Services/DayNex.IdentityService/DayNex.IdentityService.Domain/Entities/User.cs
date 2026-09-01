using DayNex.Shared.Contracts;
using DayNex.Shared.Contracts.Enums;

namespace DayNex.IdentityService.Domain.Entities;

/// <summary>
/// Local record of a user, synced from Entra External ID on first login (via the
/// external subject id). Identity/authentication itself lives in Entra — this entity
/// only owns role + profile data that DayNex needs to reason about.
/// </summary>
public class User : BaseEntity
{
    public string ExternalId { get; private set; } = default!; // Entra "sub"/"oid" claim
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Subscription? Subscription { get; private set; }

    private User() { } // EF Core

    private User(string externalId, string email, string displayName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        ExternalId = externalId;
        Email = email.Trim().ToLowerInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        Role = role;
    }

    /// <summary>Factory used on first-login provisioning. New users default to SimpleUser.</summary>
    public static User CreateSimpleUser(string externalId, string email, string displayName)
        => new(externalId, email, displayName, UserRole.SimpleUser);

    /// <summary>Only ever called for the seeded owner account — never exposed via a public API.</summary>
    public static User CreateSuperAdmin(string externalId, string email, string displayName)
        => new(externalId, email, displayName, UserRole.SuperAdmin);

    public void ChangeRole(UserRole newRole)
    {
        if (Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("SuperAdmin role cannot be changed.");
        if (newRole == UserRole.SuperAdmin)
            throw new InvalidOperationException("SuperAdmin cannot be granted through role update.");

        Role = newRole;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void AttachSubscription(Subscription subscription)
    {
        Subscription = subscription;
        MarkUpdated();
    }
}
