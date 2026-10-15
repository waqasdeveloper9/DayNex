using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DayNex.UserInfoService.Application;
public static class UserInfoApplicationDependencies
{
    public static IServiceCollection AddUserInfoApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerAssetService, CustomerAssetService>();
        services.AddScoped<VehicleMotService>();
        services.AddScoped<CompanyProfileService>();
        services.AddScoped<UserLocationService>();
        services.AddScoped<UserInfoOnboardingService>();
        services.AddScoped<VehicleRefreshService>();
        services.AddScoped<VehicleRefreshJob>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
