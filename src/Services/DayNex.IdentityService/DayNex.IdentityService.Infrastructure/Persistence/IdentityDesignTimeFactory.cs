using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DayNex.IdentityService.Infrastructure.Persistence;

public sealed class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("DayNex-IdentityService-Development")
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        // Building the model opens no database connection. EF database commands use the same secret as the API.
        return new(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection,
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)).Options);
    }
}
