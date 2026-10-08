using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DayNex.IdentityService.Application.Interface;
using Microsoft.Extensions.Options;

namespace DayNex.IdentityService.Infrastructure
{
    public static class IdentityInfrastructureDependencies
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<IdentityDatabaseOptions>().Bind(configuration.GetSection("ConnectionStrings"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection), "Configure ConnectionStrings:DefaultConnection using secrets or environment variables.")
                .ValidateOnStart();
            services.AddDbContextPool<IdentityDbContext>((provider, options) =>
               options.UseSqlServer(provider.GetRequiredService<IOptions<IdentityDatabaseOptions>>().Value.DefaultConnection,
                   sql => sql.EnableRetryOnFailure(3)), poolSize: 128);

            services.AddScoped(typeof(IGenericRepository<>), typeof(IdentityEfRepository<>));
            services.AddScoped<IIdentityStore, IdentityStore>();
            services.AddScoped<ICustomerPreferencesStore, CustomerPreferencesStore>();
            return services;

        }
    }

    public sealed class IdentityDatabaseOptions
    {
        public string DefaultConnection { get; set; } = "";
    }
}
