using System.Text.Json;
using DayNex.Shared.Http.Interface;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using Microsoft.Extensions.Options;
namespace DayNex.UserInfoService.Infrastructure;
public sealed class CompaniesHouseClient(IApiClient client,IOptions<CompaniesHouseOptions> options) : ICompanyProfileClient
{
    public async Task<CompanyLookupResult> FetchAsync(string number,CancellationToken token)
    {
        number=UserCompany.Normalize(number);
        if(!options.Value.Enabled) return new("NotConfigured");
        try {
            var json=await client.GetAsync<JsonElement>(options.Value.ProfilePath+Uri.EscapeDataString(number),token);
            return new("Available",CompanyProfileResponse.Map(json,number));
        }
        catch(Exception error) when(ProviderFailure.Handles(error,token)) {return new(ProviderFailure.Status(error));}
    }
}
