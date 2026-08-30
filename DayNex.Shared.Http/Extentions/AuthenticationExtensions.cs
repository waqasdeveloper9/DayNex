using DayNex.Shared.Http.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DayNex.Shared.Http.Extentions
{
    /// <summary>
    /// Every DayNex microservice calls AddDayNexAuthentication(configuration) in Program.cs
    /// once, and gets identical JWT validation against Entra External ID plus the shared
    /// authorization policies (Admin, SuperAdmin, Premium). This is what keeps the
    /// architecture leak-proof — no service reimplements auth, they all consume it from here.
    /// Requires appsettings section:
    ///   "EntraExternalId": { "Authority": "...", "Audience": "...", "Instance": "..." }
    /// </summary>
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddDayNexAuthentication(
            this IServiceCollection services, IConfiguration configuration)
        {
            var authority = configuration["EntraExternalId:Authority"]
                ?? throw new InvalidOperationException("EntraExternalId:Authority is not configured");
            var audience = configuration["EntraExternalId:Audience"]
                ?? throw new InvalidOperationException("EntraExternalId:Audience is not configured");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RoleClaimType = DayNexClaimTypes.Role,
                        NameClaimType = DayNexClaimTypes.UserId
                    };
                });

            return services;
        }

        public static IServiceCollection AddDayNexAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, SuperAdminBypassHandler>();
            services.AddSingleton<IAuthorizationHandler, PremiumTierHandler>();

            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.RequireAdmin, policy =>
                    policy.RequireRole(
                        Domain.Common.Enums.UserRole.Admin.ToString(),
                        Domain.Common.Enums.UserRole.SuperAdmin.ToString()))
                .AddPolicy(PolicyNames.RequireSuperAdmin, policy =>
                    policy.RequireRole(Domain.Common.Enums.UserRole.SuperAdmin.ToString()))
                .AddPolicy(PolicyNames.RequirePremium, policy =>
                    policy.Requirements.Add(new PremiumTierRequirement()));

            return services;
        }
    }

}
