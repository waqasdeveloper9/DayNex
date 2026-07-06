using System.Text.Json.Serialization;

namespace DayNex.HolidayService.Application.DTOs
{
    public class GovUkBankHolidayResponseDto
    {
        [JsonPropertyName("england-and-wales")]
        public GovUkDivisionDto? EnglandAndWales { get; set; }

        [JsonPropertyName("scotland")]
        public GovUkDivisionDto? Scotland { get; set; }

        [JsonPropertyName("northern-ireland")]
        public GovUkDivisionDto? NorthernIreland { get; set; }
    }

    public class GovUkEventDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("bunting")]
        public bool Bunting { get; set; }
    }
    public class GovUkDivisionDto
    {
        [JsonPropertyName("division")]
        public string Division { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<GovUkEventDto> Events { get; set; } = [];
    }

}
