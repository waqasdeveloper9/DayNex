using DayNex.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DayNex.IdentityService.Infrastructure.Persistence.Configurations;
public sealed class CustomerPreferencesConfiguration : IEntityTypeConfiguration<CustomerPreferences>
{
    public void Configure(EntityTypeBuilder<CustomerPreferences> builder)
    {
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.Postcode).HasMaxLength(10).IsRequired();
        builder.Property(p => p.HolidayRegion).HasMaxLength(20).IsRequired();
        builder.HasOne<User>().WithOne().HasForeignKey<CustomerPreferences>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
