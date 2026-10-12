using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DayNex.VehicleService.Application;
public static class VehicleApplicationDependencies
{
    public static IServiceCollection AddVehicleApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerAssetService, CustomerAssetService>();
        services.AddScoped<VehicleMotService>();
        services.AddScoped<VehicleRefreshService>();
        services.AddScoped<VehicleRefreshJob>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
