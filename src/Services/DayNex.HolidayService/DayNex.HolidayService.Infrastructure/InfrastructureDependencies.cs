using DayNex.Domain.Common.Interface;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.Services;
using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Infrastructure.ExternalApi;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Persistence.Repositories;
using DayNex.HolidayService.Infrastructure.Setting;
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
            services.AddScoped<IRepository<BankHoliday>, BankHolidayRepository>();
            services.AddScoped<IBankHoliday, BankHolidayService>();
            services.Configure<GovUkApiSettings>(configuration.GetSection("GovUkApiSettings"));
            return services;
       
        }
     }
}
