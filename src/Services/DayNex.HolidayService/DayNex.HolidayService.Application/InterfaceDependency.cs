using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.Services;
using Microsoft.Extensions.DependencyInjection;


namespace DayNex.HolidayService.Application
{
    public static class InterfaceDependency
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IBankHoliday, BankHolidayService>();

            //services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}
