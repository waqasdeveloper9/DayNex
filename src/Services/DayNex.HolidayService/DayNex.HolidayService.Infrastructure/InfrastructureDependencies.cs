using DayNex.Domain.Common.Interface;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Infrastructure.ExternalApi;
using DayNex.HolidayService.Infrastructure.Persistence;
using DayNex.HolidayService.Infrastructure.Setting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DayNex.HolidayService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Options;

namespace DayNex.HolidayService.Infrastructure
{
    public static class InfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<HolidayDatabaseOptions>().Bind(configuration.GetSection("ConnectionStrings"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection), "Configure ConnectionStrings:DefaultConnection using secrets or environment variables.")
                .ValidateOnStart();
            services.AddDbContextPool<HolidayDbContext>((provider, options) =>
               options.UseSqlServer(provider.GetRequiredService<IOptions<HolidayDatabaseOptions>>().Value.DefaultConnection,
                   sql => sql.EnableRetryOnFailure(3)), poolSize: 128);


            services.AddScoped<IGovUkHolidayApiClient, GovUkHolidayApiClient>();
            services.AddScoped<IHolidaySyncStore, HolidaySyncStore>();
            services.AddScoped<IUpcomingHolidayQuery, UpcomingHolidayQuery>();
            services.AddSingleton(TimeProvider.System);
            services.AddOptions<GovUkApiSettings>().Bind(configuration.GetSection("GovUkApiSettings"))
                .Validate(o => Uri.TryCreate(o.BankHolidaysUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
                    "GovUkApiSettings:BankHolidaysUrl must be an absolute HTTPS URL.")
                .ValidateOnStart();
            services.AddScoped(typeof(IGenericRepository<>), typeof(HolidayEfRepository<>));
            return services;

        }
    }

    public sealed class HolidayDatabaseOptions
    {
        public string DefaultConnection { get; set; } = "";
    }
}
