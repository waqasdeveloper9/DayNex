using DayNex.Shared.Http.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DayNex.Shared.Http.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IPermissionSource source, IHttpContextAccessor accessor, IOptions<EntraOptions> entra)
    : AuthorizationHandler<PermissionRequirement>
{
    // Scoped handler: a request makes at most one permission lookup, with no stale cross-request grants.
    private Task<DayNex.Contracts.Identity.UserPermissionsDto?>? _lookup;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || ExternalIdentity.FromPrincipal(context.User) is null ||
            !AuthenticationDependencies.HasScope(context.User, entra.Value.RequiredScope))
            return;
        var cancellation = accessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var permissions = await (_lookup ??= source.GetAsync(context.User, cancellation));
        if (permissions?.Permissions.Contains(requirement.Permission, StringComparer.Ordinal) == true)
            context.Succeed(requirement);
    }
}

public static class AuthorizationDependencies
{
    public static IServiceCollection AddDayNexPermissions(this IServiceCollection services, params string[] permissions)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var permission in permissions)
                options.AddPolicy(permission, policy => policy.Combine(options.GetPolicy(AuthenticationDependencies.ApiAccess)!)
                    .AddRequirements(new PermissionRequirement(permission)));
        });
        return services;
    }
}
