using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DayNex.UserInfoService.Infrastructure;
public sealed class UserInfoDesignTimeFactory : IDesignTimeDbContextFactory<UserInfoDbContext>
{
    public UserInfoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("DayNex-VehicleService-Development").AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        // Building the model opens no database connection, matching the other services.
        return new UserInfoDbContext(new DbContextOptionsBuilder<UserInfoDbContext>().UseSqlServer(connection, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)).Options);
    }
}
