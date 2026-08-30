using DayNex.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DayNex.Shared.Http.Authorization
{
    public class SuperAdminBypassHandler : AuthorizationHandler<IAuthorizationRequirement>
    {

        /// <summary>
        /// Registered once per microservice. Runs against EVERY authorization requirement
        /// (role checks, tier checks, future custom policies) and auto-succeeds if the caller
        /// is SuperAdmin — implementing "SuperAdmin bypasses all rights/subscription checks"
        /// in exactly one place instead of scattering `if (role == SuperAdmin)` everywhere.
        /// </summary>
        
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
        {
            if (context.User.IsInRole(UserRole.SuperAdmin.ToString()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

}
