using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DayNex.UserInfoService.Infrastructure;
public sealed class UserInfoDbContext(DbContextOptions<UserInfoDbContext> options) : DbContext(options)
{
    public DbSet<UserVehicle> Vehicles => Set<UserVehicle>();
    public DbSet<UserCompany> Companies => Set<UserCompany>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<UserLocation> Locations => Set<UserLocation>();

    public DbSet<MotVehicleHistory> MotHistories => Set<MotVehicleHistory>();
    public DbSet<MotTest> MotTests => Set<MotTest>();

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
        vehicles.Property(v => v.MotLookupStatus).HasMaxLength(32).IsRequired();
        vehicles.Property(v => v.MotLastAttemptUtc).HasConversion(
            value => value.HasValue ? (long?)value.Value.UtcTicks : null,
            value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        vehicles.HasOne(v => v.MotHistory).WithOne().HasForeignKey<MotVehicleHistory>(history => history.Id)
            .OnDelete(DeleteBehavior.Cascade);
        model.ApplyConfigurationsFromAssembly(typeof(UserInfoDbContext).Assembly);
        vehicles.HasIndex(v => new { v.ScheduledAttemptUtcTicks, v.Id });
        var job = model.Entity<VehicleRefreshJobState>();
        job.ToTable("VehicleRefreshJobState");
        job.HasKey(j => j.Id);
        job.Property(j => j.Id).ValueGeneratedNever();
        job.HasData(new VehicleRefreshJobState());
        var companies = model.Entity<UserCompany>();
        companies.ToTable("UserCompanies");
        companies.HasKey(v => v.Id);
        companies.Property(v => v.CompanyNumber).HasMaxLength(8).IsRequired();
        companies.Property(v => v.LookupStatus).HasMaxLength(32).HasDefaultValue("Pending");
        companies.Property(v => v.LastAttemptUtc).HasConversion(v => v.HasValue ? (long?)v.Value.UtcTicks : null, v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);
        companies.HasOne(v => v.Profile).WithOne().HasForeignKey<CompanyProfile>(v => v.Id).OnDelete(DeleteBehavior.Cascade);
        companies.HasIndex(v => new { v.UserId, v.CompanyNumber }).IsUnique();
    // UserId is a logical reference to Identity; no cross-database foreign key.
    }
}
