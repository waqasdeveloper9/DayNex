using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayNex.UserInfoService.Infrastructure.Persistence.Configurations;

public sealed class MotDefectConfiguration : IEntityTypeConfiguration<MotDefect>
{
    public void Configure(EntityTypeBuilder<MotDefect> defect)
    {
        defect.ToTable("VehicleMotDefects");
        defect.HasKey(value => value.Id);
        defect.Property(value => value.Id).ValueGeneratedNever();
        defect.Property(value => value.Text);
        defect.Property(value => value.Type).HasMaxLength(100);
    }
}
