using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DayNex.VehicleService.Infrastructure;
public sealed class VehicleDesignTimeFactory : IDesignTimeDbContextFactory<VehicleDbContext>
{
    public VehicleDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().AddUserSecrets("DayNex-VehicleService-Development").AddEnvironmentVariables().Build();
        // Only a design-time fallback for generating SQL; no automatic database creation or migration.
        var connection = config.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\MSSQLLocalDB;Database=DayNexVehicleDesign;Trusted_Connection=True;Encrypt=True";
        return new(new DbContextOptionsBuilder<VehicleDbContext>().UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3)).Options);
    }
}
