namespace DayNex.UserInfoService.Infrastructure;
public sealed class CompaniesHouseOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.company-information.service.gov.uk/";
    public string ProfilePath { get; set; } = "company/";
    public string ApiKey { get; set; } = "";
}
public sealed class CouncilOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://www.gov.uk/";
    public string LookupPath { get; set; } = "api/local-authority";
}
