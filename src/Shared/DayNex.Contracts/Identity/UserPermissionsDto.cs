namespace DayNex.Contracts.Identity;

public sealed record UserPermissionsDto(Guid UserId, string[] Permissions);

public static class DayNexPermissions
{
    public const string HolidaySync = "holidays.sync";
}
