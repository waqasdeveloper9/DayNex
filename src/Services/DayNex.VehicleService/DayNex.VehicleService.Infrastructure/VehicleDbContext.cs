using DayNex.VehicleService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DayNex.VehicleService.Infrastructure;
public sealed class VehicleDbContext(DbContextOptions<VehicleDbContext> options) : DbContext(options)
{
    public DbSet<UserVehicle> Vehicles => Set<UserVehicle>();
    public DbSet<UserCompany> Companies => Set<UserCompany>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var vehicles = model.Entity<UserVehicle>();
        vehicles.ToTable("UserVehicles");
        vehicles.HasKey(v => v.Id);
        vehicles.Property(v => v.RegistrationNumber).HasMaxLength(7).IsRequired();
        vehicles.HasIndex(v => new { v.UserId, v.RegistrationNumber }).IsUnique();
        vehicles.Property(v => v.LookupStatus).HasMaxLength(32).IsRequired();
        // Numeric UTC representation supports comparisons consistently in SQL Server and SQLite tests.
        vehicles.Property(v => v.LastAttemptUtc).HasConversion(v => v.HasValue ? (long? )v.Value.UtcTicks : null, v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);
        vehicles.OwnsOne(v => v.Details, details =>
        {
            details.Property(d => d.RegistrationNumber).HasMaxLength(7);
            foreach (var property in typeof(VehicleDetails).GetProperties().Where(p => p.PropertyType == typeof(string)))
                details.Property(property.Name).HasMaxLength(property.Name.StartsWith("Month") ? 7 : 100);
        });
        var companies = model.Entity<UserCompany>();
        companies.ToTable("UserCompanies");
        companies.HasKey(v => v.Id);
        companies.Property(v => v.CompanyNumber).HasMaxLength(8).IsRequired();
        companies.HasIndex(v => new { v.UserId, v.CompanyNumber }).IsUnique();
    // UserId is a logical reference to Identity; no cross-database foreign key.
    }
}
