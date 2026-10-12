namespace DayNex.VehicleService.Infrastructure;

public sealed class MotOptions
{
    public const string Section = "Mot";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
    public string RegistrationPath { get; set; } = "";
    public string TokenEndpoint { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
