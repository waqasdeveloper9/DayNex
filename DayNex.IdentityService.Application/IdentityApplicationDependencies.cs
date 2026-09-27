using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.IdentityService.Infrastructure
{
    public static class IdentityApplicationDependencies
    {
        public static IServiceCollection AddIdentityApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserProfileService, UserProfileService>();
            return services;

        }
    }
}
