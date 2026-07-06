using DayNex.HolidayService.Domain.Enums;

namespace DayNex.HolidayService.Domain.Entities
{
    public class BankHoliday 
    {
        private BankHoliday()
        {
        }

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

        public Region Region { get; set; }
        public DateOnly Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool Bunting { get; set; }
    }
}
