using DayNex.HolidayService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayNex.HolidayService.Infrastructure.Persistence
{
    public class HolidayDbContext : DbContext
    {
        public HolidayDbContext(DbContextOptions<HolidayDbContext> options)
            : base(options)
        {
        }

        public DbSet<BankHoliday> BankHoliday { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional but recommended: entity configuration yahan ya alag IEntityTypeConfiguration file mein
            modelBuilder.Entity<BankHoliday>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                // baqi property configurations agar chahiye
            });
        }
    }
}