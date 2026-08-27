namespace DayNex.HolidayService.Application.DTOs.UpcomingHolidaysDto
{
    public class UpcomingHolidayResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public int DaysUntil { get; set; }
        public bool IsBunting { get; set; }
        public string? Notes { get; set; }
        public bool IsGrouped { get; set; }
        public int? GroupTotalDays { get; set; }
        public List<Guid>? GroupHolidayIds { get; set; }
    }

    public class UpcomingHolidaysResponseDto
    {
        public List<UpcomingHolidayResponseDto> Holidays { get; set; } = new();
    }
}
