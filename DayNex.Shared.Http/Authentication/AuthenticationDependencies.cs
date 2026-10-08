using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace DayNex.Shared.Http.Authentication;

public static partial class AuthenticationDependencies
{
    public const string ApiAccess = "DayNex.ApiAccess";

    public static IServiceCollection AddDayNexAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EntraOptions>().Bind(configuration.GetSection(EntraOptions.SectionName))
            .Validate(o => TenantName().IsMatch(o.TenantSubdomain), "Entra:TenantSubdomain must be the external tenant's subdomain.")
            .Validate(o => o.TenantId != Guid.Empty && o.Audience != Guid.Empty, "Entra tenant ID and API audience/client ID are required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RequiredScope) && !o.RequiredScope.Any(char.IsWhiteSpace), "Entra:RequiredScope must be one scope name.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<EntraOptions>>((options, settings) =>
            {
                var entra = settings.Value;
                options.Authority = entra.Authority;
                options.Audience = entra.Audience.ToString("D");
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var principal = context.Principal!;
                        if (principal.FindFirst("ver")?.Value != "2.0" ||
                            !Guid.TryParse(principal.FindFirst("tid")?.Value, out var tenant) || tenant != entra.TenantId ||
                            ExternalIdentity.FromPrincipal(principal) is null)
                            context.Fail("The token is not a supported customer access token.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            var scope = configuration.GetSection(EntraOptions.SectionName).Get<EntraOptions>()?.RequiredScope ?? "access_as_user";
            var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(c => HasScope(c.User, scope))
                .Build();
            options.AddPolicy(ApiAccess, policy);
            options.DefaultPolicy = policy;
            options.FallbackPolicy = policy;
        });
        return services;
    }

    public static bool HasScope(ClaimsPrincipal principal, string scope) => principal.FindAll("scp").Any(claim =>
        claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope, StringComparer.Ordinal));

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantName();
}
