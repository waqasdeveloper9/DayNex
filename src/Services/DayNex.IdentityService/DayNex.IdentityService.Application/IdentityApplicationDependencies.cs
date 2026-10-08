using DayNex.IdentityService.Application.Interface;
using DayNex.IdentityService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DayNex.IdentityService.Application
{
    public static class IdentityApplicationDependencies
    {
        public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<ICustomerPreferencesService, CustomerPreferencesService>();
            services.TryAddSingleton(TimeProvider.System);
            return services;

        }
    }
}
