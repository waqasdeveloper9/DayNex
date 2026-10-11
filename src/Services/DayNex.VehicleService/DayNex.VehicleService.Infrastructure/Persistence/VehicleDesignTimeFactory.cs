using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DayNex.VehicleService.Infrastructure;
public sealed class VehicleDesignTimeFactory : IDesignTimeDbContextFactory<VehicleDbContext>
{
    public VehicleDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("DayNex-VehicleService-Development").AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        // Building the model opens no database connection, matching the other services.
        return new VehicleDbContext(new DbContextOptionsBuilder<VehicleDbContext>().UseSqlServer(connection, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)).Options);
    }
}
