using System.Text.Json.Serialization;
namespace DayNex.UserInfoService.Infrastructure;
internal sealed record CouncilResponse([property:JsonPropertyName("local_authority")] LocalAuthorityResponse? LocalAuthority);
internal sealed record LocalAuthorityResponse(string? Name,[property:JsonPropertyName("homepage_url")] string? HomepageUrl,string? Tier,string? Slug);
