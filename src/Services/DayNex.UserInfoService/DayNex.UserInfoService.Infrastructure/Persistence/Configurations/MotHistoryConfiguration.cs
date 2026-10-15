using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayNex.UserInfoService.Infrastructure.Persistence.Configurations;

public sealed class MotHistoryConfiguration : IEntityTypeConfiguration<MotVehicleHistory>
{
    public void Configure(EntityTypeBuilder<MotVehicleHistory> history)
    {
        history.ToTable("VehicleMotHistories");
        history.HasKey(snapshot => snapshot.Id);
        history.Property(snapshot => snapshot.Id).ValueGeneratedNever();
        history.Property(snapshot => snapshot.Registration).HasMaxLength(7).IsRequired();
        history.Property(snapshot => snapshot.Make).HasMaxLength(200);
        history.Property(snapshot => snapshot.Model).HasMaxLength(200);
        history.Property(snapshot => snapshot.FuelType).HasMaxLength(100);
        history.Property(snapshot => snapshot.PrimaryColour).HasMaxLength(100);
        history.Property(snapshot => snapshot.ManufactureYear).HasMaxLength(4);
        history.Property(snapshot => snapshot.EngineSize).HasMaxLength(100);
        history.Property(snapshot => snapshot.HasOutstandingRecall).HasMaxLength(32);
        history.HasMany(snapshot => snapshot.Tests).WithOne().HasForeignKey(test => test.HistoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
