using DayNex.Domain.Common.Interface;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.Services;
using DayNex.HolidayService.Infrastructure.ExternalApi;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Setting;
using DayNex.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.HolidayService.Infrastructure
{
    public static class InfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<HolidayDbContext>(options =>
               options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));


            services.AddScoped<IGovUkHolidayApiClient, GovUkHolidayApiClient>();
            services.AddScoped<IBankHoliday, BankHolidayService>();
            services.Configure<GovUkApiSettings>(configuration.GetSection("GovUkApiSettings"));
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            return services;

        }
    }
}
