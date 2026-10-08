using System.Security.Claims;

namespace DayNex.Shared.Http.Authentication;

public sealed record ExternalIdentity(string Key, string? Email, string? DisplayName)
{
    // oid is stable across app registrations; email and display name are not identifiers.
    public static ExternalIdentity? FromPrincipal(ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(principal.FindFirstValue("tid"), out var tenantId) ||
            !Guid.TryParse(principal.FindFirstValue("oid"), out var objectId) ||
            tenantId == Guid.Empty || objectId == Guid.Empty)
            return null;

        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue("emails");
        return new($"{tenantId:D}:{objectId:D}", email, principal.FindFirstValue("name"));
    }
}
