using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DayNex.Tests;

internal static class TestAuthentication
{
    internal static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid Audience = Guid.Parse("22222222-2222-2222-2222-222222222222");
    internal const string Issuer = "https://test.ciamlogin.com/11111111-1111-1111-1111-111111111111/v2.0";
    internal static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "integration-test-key" };

    internal static Dictionary<string, string?> Configuration => new()
    {
        ["Entra:TenantSubdomain"] = "test",
        ["Entra:TenantId"] = Tenant.ToString(),
        ["Entra:Audience"] = Audience.ToString(),
        ["Entra:RequiredScope"] = "access_as_user",
        ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true",
        ["IdentityService:BaseUrl"] = "https://identity.test/"
    };

    internal static void Configure(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
            metadata.SigningKeys.Add(Key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        });
    }

    internal static string Token(Guid user, string? scope = "access_as_user", string? audience = null,
        string? issuer = null, bool expired = false, SecurityKey? key = null, bool includeIdentity = true,
        bool includeProfile = true, Guid? tenant = null)
    {
        var claims = new List<Claim> { new("ver", "2.0"), new("tid", (tenant ?? Tenant).ToString()) };
        if (includeIdentity) claims.Add(new("oid", user.ToString()));
        if (scope is not null) claims.Add(new("scp", scope));
        if (includeProfile)
        {
            claims.Add(new("email", "person@example.test"));
            claims.Add(new("name", "Test customer"));
        }
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(issuer ?? Issuer, audience ?? Audience.ToString(), claims,
            now.AddHours(-1), expired ? now.AddMinutes(-5) : now.AddMinutes(10),
            new SigningCredentials(key ?? Key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
