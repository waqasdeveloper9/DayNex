using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DayNex.Shared.Http;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using DayNex.UserInfoService.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
namespace DayNex.Tests;

public sealed class TestCompanyClient : ICompanyProfileClient
{
    public int Calls {get;private set;}
    public bool Fail {get;set;}
    public Task<CompanyLookupResult> FetchAsync(string number,CancellationToken token) {
        Calls++;
        return Task.FromResult(Fail ? new CompanyLookupResult("Unavailable") : new("Available",new CompanyProfile {
            CompanyNumber=number,CompanyName="Example Ltd",CompanyStatus="active",AccountsDueOn=new(2027,4,1),
            RegisteredOffice=new() {PostalCode="BD15 7QW"},SicCodes=[new(){Code="62012"}],
            PreviousNames=[new(){Name="Old Example Ltd"}],RawResponseJson="{\"company_name\":\"Example Ltd\"}"
        }));
    }
}
public sealed class TestCouncilClient : ICouncilClient
{
    public int Calls {get;private set;}
    public bool Fail {get;set;}
    public Task<CouncilLookupResult> FetchAsync(string postcode,CancellationToken token) {
        Calls++;
        return Task.FromResult(Fail ? new CouncilLookupResult("Unavailable") : new("Available",new UserCouncil {
            Name=postcode=="BD15 7QW"?"Bradford":"Example Council",HomepageUrl="https://www.bradford.gov.uk/",Tier="unitary",Slug="bradford"
        }));
    }
}
public sealed class UserInfoTests
{
    [Fact]
    public async Task OnboardingEnrichesOnlyProvidedDataAndIsIdempotentAndOwnerScoped()
    {
        await using var factory=new VehicleFactory();using var client=factory.Client();
        const string path="/api/assets/me";
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync(path+"/onboarding",new CompleteUserInfoRequest())).StatusCode);
        var owner=Guid.NewGuid();client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(owner));
        (await client.PostAsJsonAsync(path+"/onboarding",new CompleteUserInfoRequest())).EnsureSuccessStatusCode();
        Assert.Equal(0,factory.Company.Calls);Assert.Equal(0,factory.Council.Calls);Assert.Equal(0,factory.Lookup.Calls);
        var input=new CompleteUserInfoRequest("bd157qw",["AB12CDE"],["sc123456","SC123456"]);
        var response=await client.PostAsJsonAsync(path+"/onboarding",input);response.EnsureSuccessStatusCode();
        var result=(await response.Content.ReadFromJsonAsync<CustomerAssetsDto>())!;
        Assert.Single(result.Companies);Assert.Single(result.Vehicles);Assert.Equal("Example Ltd",result.Companies[0].Profile!.CompanyName);
        Assert.Equal("BD15 7QW",result.Location!.Postcode);Assert.Equal("Bradford",result.Location.Council!.Name);
        (await client.PostAsJsonAsync(path+"/onboarding",input)).EnsureSuccessStatusCode();
        Assert.Equal(1,factory.Company.Calls);Assert.Equal(1,factory.Council.Calls);Assert.Equal(1,factory.Lookup.Calls);
        var id=result.Companies[0].Id;
        Assert.Equal(HttpStatusCode.TooManyRequests,(await client.PostAsync(path+$"/companies/{id}/refresh",null)).StatusCode);
        client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(path+$"/companies/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync(path+$"/companies/{id}/refresh",null)).StatusCode);
        var other=(await client.GetFromJsonAsync<CustomerAssetsDto>(path))!;Assert.Empty(other.Companies);Assert.Null(other.Location);
    }
    [Fact]
    public async Task InvalidBatchIsRejectedBeforeAnyProviderCallAndFailureKeepsReferences()
    {
        await using var factory=new VehicleFactory();using var client=factory.Client();
        client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid()));
        var invalid=new CompleteUserInfoRequest("BD157QW",["AB12CDE"],["../bad"]);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/assets/me/onboarding",invalid)).StatusCode);
        Assert.Equal(0,factory.Council.Calls);Assert.Equal(0,factory.Lookup.Calls);
        factory.Company.Fail=true;factory.Council.Fail=true;
        var response=await client.PostAsJsonAsync("/api/assets/me/onboarding",new CompleteUserInfoRequest("BD157QW",CompanyNumbers:["00000006"]));
        response.EnsureSuccessStatusCode();var result=(await response.Content.ReadFromJsonAsync<CustomerAssetsDto>())!;
        Assert.Equal("Unavailable",result.Companies.Single().LookupStatus);Assert.Null(result.Companies[0].Profile);
        Assert.Equal("Unavailable",result.Location!.LookupStatus);Assert.Equal("BD15 7QW",result.Location.Postcode);
    }
    [Fact]
    public async Task ChangedOrClearedPostcodeNeverDisplaysTheOldCouncil()
    {
        await using var factory=new VehicleFactory();using var client=factory.Client();
        client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid()));
        (await client.PutAsJsonAsync("/api/assets/me/location",new UpdateLocationRequest("BD157QW"))).EnsureSuccessStatusCode();
        factory.Council.Fail=true;
        var response=await client.PutAsJsonAsync("/api/assets/me/location",new UpdateLocationRequest("SW1A1AA"));response.EnsureSuccessStatusCode();
        var changed=(await response.Content.ReadFromJsonAsync<LocationDto>())!;
        Assert.Equal("SW1A 1AA",changed.Postcode);Assert.Null(changed.Council);Assert.Null(changed.RetrievedUtc);
        response=await client.PutAsJsonAsync("/api/assets/me/location",new UpdateLocationRequest(""));response.EnsureSuccessStatusCode();
        var cleared=(await response.Content.ReadFromJsonAsync<LocationDto>())!;
        Assert.Equal("NotProvided",cleared.LookupStatus);Assert.Null(cleared.Council);Assert.Equal(2,factory.Council.Calls);
    }
    [Fact]
    public async Task LateCouncilResponseCannotOverwriteNewPostcodeEvenAfterChangingBack()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        await using var db=new UserInfoDbContext(new DbContextOptionsBuilder<UserInfoDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();var store=new UserLocationStore(db);var id=Guid.NewGuid();var now=DateTimeOffset.UtcNow;
        var first=await store.SetPostcodeAsync(id,"BD157QW",now,default);var oldRevision=first.Revision;
        Assert.True(await store.TryBeginAsync(id,oldRevision,now.UtcTicks,default));
        await store.SetPostcodeAsync(id,"SW1A1AA",now.AddSeconds(1),default);
        var latest=await store.SetPostcodeAsync(id,"BD157QW",now.AddSeconds(2),default);
        Assert.NotEqual(oldRevision,latest.Revision);
        Assert.False(await store.SaveAsync(id,oldRevision,now.UtcTicks,new("Available",new(){Name="Stale",Slug="stale"}),now.AddSeconds(3),default));
        Assert.Null((await store.FindAsync(id,default))!.Council);
    }
    [Fact]
    public async Task CompanySnapshotsReplaceChildrenPreserveOnFailureAndCascadeOnDelete()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        await using var db=new UserInfoDbContext(new DbContextOptionsBuilder<UserInfoDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();var writer=new AssetWriter(db);var store=new CompanyProfileStore(db);var now=DateTimeOffset.UtcNow;
        var company=await writer.AddCompanyAsync(UserCompany.Create(Guid.NewGuid(),"00000006",now),default);
        for(var i=0;i<2;i++) {
            var attempt=now.AddMinutes(i*6);Assert.True(await store.TryBeginAsync(company.UserId,company.Id,attempt,default));
            Assert.False(await store.TryBeginAsync(company.UserId,company.Id,attempt.AddSeconds(1),default));
            var p=new CompanyProfile {CompanyNumber=company.CompanyNumber,CompanyName="Name "+i,RawResponseJson="{}",
                RegisteredOffice=new(){PostalCode="BD15 7QW"},SicCodes=[new(){Code="62012"}],PreviousNames=[new(){Name="Old"}]};
            Assert.True(await store.SaveAsync(company.UserId,company.Id,attempt,new("Available",p),attempt,default));
        }
        Assert.Equal(1,await db.Set<CompanySicCode>().CountAsync());Assert.Equal(1,await db.Set<CompanyPreviousName>().CountAsync());
        var last=now.AddMinutes(12);await store.TryBeginAsync(company.UserId,company.Id,last,default);
        await store.SaveAsync(company.UserId,company.Id,last,new("Unavailable"),last,default);
        var row=(await store.FindAsync(company.UserId,company.Id,default))!;Assert.Equal("Name 1",row.Profile!.CompanyName);Assert.Equal("Unavailable",row.LookupStatus);
        Assert.Null(await store.FindAsync(Guid.NewGuid(),company.Id,default));
        await writer.RemoveCompanyAsync(company.UserId,company.Id,default);
        Assert.Empty(await db.CompanyProfiles.ToListAsync());Assert.Empty(await db.Set<CompanySicCode>().ToListAsync());
        Assert.False(await store.SaveAsync(company.UserId,company.Id,last,new("Unavailable"),last,default));
    }
    [Theory]
    [InlineData(401,"ProviderAccessDenied")][InlineData(403,"ProviderAccessDenied")][InlineData(404,"NotFound")]
    [InlineData(429,"RateLimited")][InlineData(503,"Unavailable")]
    public async Task CompanyProviderErrorsAreSafeStatuses(int status,string expected)
    {
        using var http=new HttpClient(new ProviderResponse((HttpStatusCode)status,"{}")){BaseAddress=new("https://api.company-information.service.gov.uk/")};
        var client=new CompaniesHouseClient(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new CompaniesHouseOptions{Enabled=true}));
        Assert.Equal(expected,(await client.FetchAsync("00000006",default)).Status);
    }
    [Fact]
    public async Task CompanyResponseMapsModernDeadlinesKeepsUnknownFieldsAndLeadingZeros()
    {
        const string json="""{"company_number":"00000006","company_name":"Example Ltd","accounts":{"next_accounts":{"due_on":"2027-03-01","overdue":false}},"confirmation_statement":{"next_due":"2026-12-01"},"sic_codes":["62012","62012"],"future_field":{"value":42}}""";
        var handler=new ProviderResponse(HttpStatusCode.OK,json);
        using var http=new HttpClient(handler){BaseAddress=new("https://api.company-information.service.gov.uk/")};
        var client=new CompaniesHouseClient(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new CompaniesHouseOptions{Enabled=true}));
        var result=await client.FetchAsync("00000006",default);
        Assert.Equal("Available",result.Status);Assert.Equal("/company/00000006",handler.Uri!.AbsolutePath);
        Assert.Equal(new DateOnly(2027,3,1),result.Profile!.AccountsDueOn);Assert.Null(result.Profile.RegisteredOffice);
        Assert.Single(result.Profile.SicCodes);Assert.Contains("future_field",result.Profile.RawResponseJson);
    }
    [Theory]
    [InlineData("{}")][InlineData("{\"company_number\":\"99999999\",\"company_name\":\"Wrong\"}")]
    [InlineData("{\"company_number\":\"00000006\",\"company_name\":\"Name\",\"date_of_creation\":\"invalid\"}")]
    public async Task InvalidCompanyPayloadIsNotSaved(string json)
    {
        using var http=new HttpClient(new ProviderResponse(HttpStatusCode.OK,json)){BaseAddress=new("https://api.company-information.service.gov.uk/")};
        var result=await new CompaniesHouseClient(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new CompaniesHouseOptions{Enabled=true})).FetchAsync("00000006",default);
        Assert.Equal("InvalidResponse",result.Status);Assert.Null(result.Profile);
    }
    [Fact]
    public async Task CouncilRequestEncodesPostcodeAndMapsResponse()
    {
        var handler=new ProviderResponse(HttpStatusCode.OK,"""{"local_authority":{"name":"Bradford","homepage_url":"https://www.bradford.gov.uk/","tier":"unitary","slug":"bradford"}}""");
        using var http=new HttpClient(handler){BaseAddress=new("https://www.gov.uk/")};
        var result=await new GovUkCouncilClient(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new CouncilOptions())).FetchAsync("bd157qw",default);
        Assert.Equal("Available",result.Status);Assert.Equal("Bradford",result.Council!.Name);Assert.Contains("BD15%207QW",handler.Uri!.AbsoluteUri);
    }
    [Fact]
    public async Task CompaniesHouseUsesBasicKeyAndNoCustomerToken()
    {
        var handler=new ProviderResponse(HttpStatusCode.NotFound,"{}");var services=new ServiceCollection();services.AddLogging();
        services.AddUserInfoProviders(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["CompaniesHouse:Enabled"]="true",["CompaniesHouse:ApiKey"]="test-only-key" }).Build());
        services.AddHttpClient("UserInfo.CompaniesHouse").ConfigurePrimaryHttpMessageHandler(()=>handler);
        using var provider=services.BuildServiceProvider();
        await provider.GetRequiredService<ICompanyProfileClient>().FetchAsync("00000006",default);
        Assert.Equal("Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes("test-only-key:")),handler.Authorization);
    }
    [Theory]
    [InlineData("https://www.gov.uk/api/local-authority/bradford", true)]
    [InlineData("https://example.com/api/local-authority/bradford", false)]
    [InlineData("http://www.gov.uk/api/local-authority/bradford", false)]
    [InlineData("https://www.gov.uk/another-service", false)]
    public async Task CouncilFollowsOnlyItsCanonicalGovernmentResource(string target, bool allowed)
    {
        var transport = new RedirectResponse(target);
        using var http = new HttpClient(new GovUkCouncilRedirectHandler {InnerHandler=transport}) {BaseAddress=new("https://www.gov.uk/")};
        var result = await new GovUkCouncilClient(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new CouncilOptions()))
            .FetchAsync("BD15 7QW",default);
        Assert.Equal(allowed ? "Available" : "Unavailable",result.Status);
        Assert.Equal(allowed ? 2 : 1,transport.Calls);
    }
    private sealed class RedirectResponse(string target) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            if(Calls==1) {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location=new Uri(target);
                return Task.FromResult(response);
            }
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content=new StringContent(
                """{"local_authority":{"name":"Bradford","homepage_url":"https://www.bradford.gov.uk/","tier":"unitary","slug":"bradford"}}""",Encoding.UTF8,"application/json")});
        }
    }
    private sealed class ProviderResponse(HttpStatusCode status,string json):HttpMessageHandler
    {
        public Uri? Uri;public string? Authorization;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
            Uri=request.RequestUri;Authorization=request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(status){Content=new StringContent(json,Encoding.UTF8,"application/json")});
        }
    }
}
