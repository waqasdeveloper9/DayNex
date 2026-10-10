namespace DayNex.VehicleService.Infrastructure;
public sealed class DvlaOptions
{
    public const string Section = "Dvla";
    public string Endpoint { get; set; } = "https://driver-vehicle-licensing.api.gov.uk/vehicle-enquiry/v1/vehicles";
    public string ApiKey { get; set; } = "";
    public bool Enabled { get; set; }
}

public sealed class VehicleDatabaseOptions
{
    public string DefaultConnection { get; set; } = "";
}

public sealed class VehicleIdentityOptions
{
    public string BaseUrl { get; set; } = "https://localhost:7166/";
}

public sealed class OutboundProxyOptions
{
    public bool Enabled { get; set; }
    public string Address { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
