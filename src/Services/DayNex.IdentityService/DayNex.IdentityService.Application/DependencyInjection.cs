using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.IdentityService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        return services;
    }
}
