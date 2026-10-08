namespace DayNex.Shared.Http.Authentication;

public sealed class EntraOptions
{
    public const string SectionName = "Entra";
    public string TenantSubdomain { get; set; } = "";
    public Guid TenantId { get; set; }
    public Guid Audience { get; set; }
    public string RequiredScope { get; set; } = "access_as_user";

    public string Authority => $"https://{TenantSubdomain}.ciamlogin.com/{TenantId:D}/v2.0";
}
