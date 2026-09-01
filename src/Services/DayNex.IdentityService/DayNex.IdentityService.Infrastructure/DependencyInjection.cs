using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Infrastructure.Persistence;
using DayNex.IdentityService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServiceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityServiceDb")
            ?? throw new InvalidOperationException("ConnectionStrings:IdentityServiceDb is not configured.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 3))); // resilient against Azure SQL transient faults

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
