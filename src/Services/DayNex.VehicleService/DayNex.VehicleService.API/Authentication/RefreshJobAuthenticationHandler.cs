using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using DayNex.VehicleService.API.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace DayNex.VehicleService.API.Authentication;

public sealed class RefreshJobAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemes, ILoggerFactory logger,
    UrlEncoder encoder, IOptions<VehicleRefreshOptions> settings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemes, logger, encoder)
{
    public const string SchemeName = "VehicleRefreshJob";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = settings.Value;
        var supplied = Request.Headers["X-DayNex-Job-Key"];
        if (!options.Enabled || string.IsNullOrEmpty(options.ApiKey) || supplied.Count != 1 || supplied[0]!.Length > 256)
            return Task.FromResult(AuthenticateResult.Fail("Job credentials required."));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!));
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(options.ApiKey));
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            return Task.FromResult(AuthenticateResult.Fail("Invalid job credentials."));
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "vehicle-refresh-scheduler")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(identity), SchemeName)));
    }
}

