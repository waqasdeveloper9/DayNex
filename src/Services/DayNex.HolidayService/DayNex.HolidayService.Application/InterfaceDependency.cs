using DayNex.HolidayService.Application.Common;
using DayNex.HolidayService.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DayNex.HolidayService.Application
{
    public static class InterfaceDependency
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IHolidayService, HolidayService>();

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}
