using DayNex.Shared.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace DayNex.IdentityService.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    SubscriptionDto? Subscription);

public class ProvisionUserDto
{
    [Required] public string ExternalId { get; set; } = default!;
    [Required, EmailAddress] public string Email { get; set; } = default!;
    [Required] public string DisplayName { get; set; } = default!;
}

public class ChangeUserRoleDto
{
    [Required] public UserRole NewRole { get; set; }
}
