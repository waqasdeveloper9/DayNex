using System.Net.Http.Headers;
using System.Text;
using DayNex.Shared.Http;
using DayNex.UserInfoService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace DayNex.UserInfoService.Infrastructure;
public static class UserInfoProviderDependencies
{
    public static IServiceCollection AddUserInfoProviders(this IServiceCollection services,IConfiguration config)
    {
        services.AddOptions<CompaniesHouseOptions>().Bind(config.GetSection("CompaniesHouse"))
            .Validate(o=>ApprovedBase(o.BaseUrl,"api.company-information.service.gov.uk","api-sandbox.company-information.service.gov.uk"),"Companies House requires an approved HTTPS base URL.")
            .Validate(o=>Relative(o.ProfilePath) && o.ProfilePath.EndsWith('/'),"CompaniesHouse:ProfilePath must be a relative directory ending in /.")
            .Validate(o=>!o.Enabled || !string.IsNullOrWhiteSpace(o.ApiKey),"Configure CompaniesHouse:ApiKey in User Secrets before enabling Companies House.").ValidateOnStart();
        services.AddOptions<CouncilOptions>().Bind(config.GetSection("Council"))
            .Validate(o=>ApprovedBase(o.BaseUrl,"www.gov.uk"),"Council requires the GOV.UK HTTPS base URL.")
            .Validate(o=>Relative(o.LookupPath),"Council:LookupPath must be a relative path without query parameters.").ValidateOnStart();
        Configure(services.AddHttpClient("UserInfo.CompaniesHouse",(sp,http)=> {
            var o=sp.GetRequiredService<IOptions<CompaniesHouseOptions>>().Value; http.BaseAddress=new(o.BaseUrl);
            if(o.Enabled) http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(o.ApiKey+":")));
        }));
        services.AddTransient<GovUkCouncilRedirectHandler>();
        Configure(services.AddHttpClient("UserInfo.Council",(sp,http)=>http.BaseAddress=new(sp.GetRequiredService<IOptions<CouncilOptions>>().Value.BaseUrl))
            .AddHttpMessageHandler<GovUkCouncilRedirectHandler>());
        services.AddScoped<ICompanyProfileClient>(sp=>new CompaniesHouseClient(Client(sp,"UserInfo.CompaniesHouse"),sp.GetRequiredService<IOptions<CompaniesHouseOptions>>()));
        services.AddScoped<ICouncilClient>(sp=>new GovUkCouncilClient(Client(sp,"UserInfo.Council"),sp.GetRequiredService<IOptions<CouncilOptions>>()));
        services.AddScoped<ICompanyProfileStore,CompanyProfileStore>();
        services.AddScoped<IUserLocationStore,UserLocationStore>();
        return services;
    }
    private static ApiClient Client(IServiceProvider sp,string name)=>new(sp.GetRequiredService<IHttpClientFactory>().CreateClient(name),sp.GetRequiredService<ILogger<ApiClient>>());
    private static void Configure(IHttpClientBuilder builder) {
        builder.ConfigureHttpClient(http=> {http.Timeout=Timeout.InfiniteTimeSpan;http.MaxResponseContentBufferSize=1024*1024;http.DefaultRequestHeaders.Accept.Add(new("application/json"));})
            .ConfigurePrimaryHttpMessageHandler(()=>new SocketsHttpHandler {AllowAutoRedirect=false,PooledConnectionLifetime=TimeSpan.FromMinutes(5)})
            .RedactLoggedHeaders(["Authorization"])
            .AddStandardResilienceHandler(o=> {o.TotalRequestTimeout.Timeout=TimeSpan.FromSeconds(15);o.AttemptTimeout.Timeout=TimeSpan.FromSeconds(4);o.Retry.MaxRetryAttempts=2;o.Retry.UseJitter=true;});
    }
    private static bool ApprovedBase(string value,params string[] hosts)=>Uri.TryCreate(value,UriKind.Absolute,out var u) && u.Scheme=="https" && u.UserInfo.Length==0 && u.IsDefaultPort && u.AbsolutePath=="/" && u.Query.Length==0 && u.Fragment.Length==0 && hosts.Contains(u.Host);
    private static bool Relative(string value)=>!string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value,UriKind.Relative,out _) && !value.StartsWith('/') && !value.Contains(':') && !value.Contains("..") && !value.Contains('\\') && !value.Contains('?') && !value.Contains('#');
}
