namespace DayNex.UserInfoService.API.Configuration;
public sealed class VehicleRefreshOptions
{
    public const string Section = "VehicleRefresh";
    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = "internal/jobs/vehicles/refresh";
    public string ApiKey { get; init; } = "";
    public int BatchSize { get; init; } = 1;
}

