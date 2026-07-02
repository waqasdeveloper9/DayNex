using DayNex.HolidayService.Domain.Enums;

namespace DayNex.HolidayService.Domain.Entities
{
    public class BankHoliday : AuditableBaseEntity<Guid>
    {
        public Region Region { get; private set; }

        public DateOnly Date { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string? Notes { get; private set; }

        public bool Bunting { get; private set; }

      

        public BankHoliday(
            Region region,
            DateOnly date,
            string title,
            string? notes,
            bool bunting)
        {
            Region = region;
            Date = date;
            Title = title;
            Notes = notes;
            Bunting = bunting;
        }
    }
}
