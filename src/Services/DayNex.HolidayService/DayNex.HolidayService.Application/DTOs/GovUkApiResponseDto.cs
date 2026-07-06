using System.Text.Json.Serialization;

namespace DayNex.HolidayService.Application.DTOs
{
    public sealed class GovUkApiResponseDto
    {
        [JsonPropertyName("england-and-wales")]
        public GovUkHolidayDivisionDto? EnglandAndWales { get; init; }

        [JsonPropertyName("scotland")]
        public GovUkHolidayDivisionDto? Scotland { get; init; }

        [JsonPropertyName("northern-ireland")]
        public GovUkHolidayDivisionDto? NorthernIreland { get; init; }
    }

    public sealed class GovUkHolidayDivisionDto
    {
        public string Division { get; init; } = string.Empty;

        public List<GovUkHolidayEventDto> Events { get; init; } = [];
    }

    public sealed class GovUkHolidayEventDto
    {
        public string Title { get; init; } = string.Empty;

        public DateOnly Date { get; init; }

        public string Notes { get; init; } = string.Empty;

        public bool Bunting { get; init; }
    }
}
