using DayNex.Domain.Common.Interface;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.IdentityService.Infrastructure
{
    public static class IdentityInfrastructureDependencies
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityDbContext>(options =>
               options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IGenericRepository<>), typeof(IdentityEfRepository<>));
            return services;

        }
    }
}
