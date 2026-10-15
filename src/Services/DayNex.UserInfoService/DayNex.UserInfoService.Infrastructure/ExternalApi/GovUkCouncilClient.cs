using System.Text.Json;
using DayNex.Shared.Http.Interface;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using Microsoft.Extensions.Options;
namespace DayNex.UserInfoService.Infrastructure;
public sealed class GovUkCouncilClient(IApiClient client,IOptions<CouncilOptions> options) : ICouncilClient
{
    public async Task<CouncilLookupResult> FetchAsync(string postcode,CancellationToken token)
    {
        postcode=UserLocation.Normalize(postcode);
        if(postcode.Length==0) return new("NotProvided");
        if(!options.Value.Enabled) return new("NotConfigured");
        try {
            var json=await client.GetAsync<JsonElement>(options.Value.LookupPath+"?postcode="+Uri.EscapeDataString(postcode),token);
            var value=json.Deserialize<CouncilResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web))?.LocalAuthority;
            if(value is null) return new("NotFound");
            if(string.IsNullOrWhiteSpace(value.Name) || string.IsNullOrWhiteSpace(value.Slug) ||
                !Uri.TryCreate(value.HomepageUrl,UriKind.Absolute,out var homepage) || homepage.Scheme!="https" || homepage.UserInfo.Length!=0)
                return new("InvalidResponse");
            // Council links are display-only data. Never fetch arbitrary provider-returned URLs.
            return new("Available",new UserCouncil {Name=value.Name,HomepageUrl=value.HomepageUrl!,Tier=value.Tier ?? "",Slug=value.Slug,RawResponseJson=json.GetRawText()});
        }
        catch(Exception error) when(ProviderFailure.Handles(error,token)) {return new(ProviderFailure.Status(error));}
    }
}
