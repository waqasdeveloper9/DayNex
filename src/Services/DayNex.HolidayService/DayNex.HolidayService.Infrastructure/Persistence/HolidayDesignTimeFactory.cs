using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DayNex.HolidayService.Infrastructure.Persistence;

public sealed class HolidayDesignTimeFactory : IDesignTimeDbContextFactory<HolidayDbContext>
{
    public HolidayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("DayNex-HolidayService-Development")
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        return new(new DbContextOptionsBuilder<HolidayDbContext>().UseSqlServer(connection,
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)).Options);
    }
}
