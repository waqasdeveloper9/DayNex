using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayNex.UserInfoService.Infrastructure.Persistence.Configurations;

public sealed class MotTestConfiguration : IEntityTypeConfiguration<MotTest>
{
    public void Configure(EntityTypeBuilder<MotTest> test)
    {
        test.ToTable("VehicleMotTests");
        test.HasKey(value => value.Id);
        test.Property(value => value.Id).ValueGeneratedNever();
        test.HasIndex(value => new { value.HistoryId, value.CompletedDate });
        test.Property(value => value.RegistrationAtTimeOfTest).HasMaxLength(32);
        test.Property(value => value.TestResult).HasMaxLength(16);
        test.Property(value => value.OdometerValue).HasMaxLength(100);
        test.Property(value => value.OdometerUnit).HasMaxLength(16);
        test.Property(value => value.OdometerResultType).HasMaxLength(32);
        test.Property(value => value.MotTestNumber).HasMaxLength(100);
        test.Property(value => value.DataSource).HasMaxLength(32);
        test.Property(value => value.Location).HasMaxLength(500);
        test.Property(value => value.CompletedDate).HasConversion(
            value => value.HasValue ? (long?)value.Value.UtcTicks : null,
            value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        test.HasMany(value => value.Defects).WithOne().HasForeignKey(value => value.TestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
