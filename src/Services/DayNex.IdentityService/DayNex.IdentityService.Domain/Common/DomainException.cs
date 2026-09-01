namespace DayNex.IdentityService.Domain.Common;

/// <summary>Thrown when a domain invariant is violated. Caught at the API boundary.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) : base($"User '{userId}' was not found.") { }
}

public class SubscriptionNotFoundException : DomainException
{
    public SubscriptionNotFoundException(Guid userId)
        : base($"No subscription found for user '{userId}'.") { }
}
