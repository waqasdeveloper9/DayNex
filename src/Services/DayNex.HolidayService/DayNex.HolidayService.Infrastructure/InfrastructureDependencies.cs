using DayNex.HolidayService.Infrastructure.Setting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.HolidayService.Infrastructure
{
    public static class InfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services.Configure<GovUkApiSettings>(configuration.GetSection("GovUkApiSettings"));
            return services;
       
        }
     }
}
