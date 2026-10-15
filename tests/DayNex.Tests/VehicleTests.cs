using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DayNex.Domain.Common.Interface;
using DayNex.Shared.Http;
using DayNex.Shared.Http.Authentication;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using DayNex.UserInfoService.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
namespace DayNex.Tests;
public sealed class TestVehicleLookup : IVehicleLookup {
 public int Calls {get;private set;}
 public bool Fail {get;set;}
 public Task<VehicleLookupResult> LookupAsync(string registration,CancellationToken token) {
  Calls++;return Task.FromResult(Fail?new VehicleLookupResult("Unavailable"):new("Available",new VehicleDetails {RegistrationNumber=registration,Make="TEST",TaxStatus="Taxed",MotExpiryDate=new(2027,3,1)}));
 }
}
public sealed class TestMotLookup : IMotHistoryClient {
 public int Calls {get; private set;}
 public Task<MotLookupResult> FetchAsync(Guid vehicleId,string registration,CancellationToken token) {
  Calls++;
  return Task.FromResult(new MotLookupResult("Available",new MotVehicleHistory(vehicleId) {
   Registration=registration,RetrievedUtc=DateTimeOffset.UtcNow,HasOutstandingRecall="No",
   Tests=[new MotTest {HistoryId=vehicleId,DataSource="DVSA",TestResult="PASSED",OdometerResultType="READ",MotTestNumber="TEST",CompletedDate=DateTimeOffset.UtcNow}]
  }));
 }
}
public sealed class TestCustomer(IHttpContextAccessor accessor) : ICurrentCustomer {
 public Task<Guid> GetUserIdAsync(CancellationToken token)=>Task.FromResult(Guid.Parse(ExternalIdentity.FromPrincipal(accessor.HttpContext!.User)!.Key.Split(':')[1]));
}
public sealed class VehicleFactory : WebApplicationFactory<DayNex.UserInfoService.API.Program> {
 private readonly SqliteConnection connection=new("Data Source=:memory:");
 public bool EnableRefresh {get;init;}
 public const string JobKey="test-job-key-at-least-32-characters-long";
 public TestCompanyClient Company {get;}=new();
 public TestCouncilClient Council {get;}=new();
 public TestVehicleLookup Lookup {get;}=new();
 public TestMotLookup Mot {get;}=new();
 protected override void ConfigureWebHost(IWebHostBuilder builder) {
  connection.Open();
  builder.UseEnvironment("Testing").ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(TestAuthentication.Configuration).AddInMemoryCollection(new Dictionary<string,string?> {["VehicleRefresh:Enabled"]=EnableRefresh.ToString(),["VehicleRefresh:ApiKey"]=JobKey}));
  builder.ConfigureServices(services=>{
   services.RemoveAll<UserInfoDbContext>();
   services.AddScoped(_=>new UserInfoDbContext(new DbContextOptionsBuilder<UserInfoDbContext>().UseSqlite(connection).Options));
   services.RemoveAll<ICurrentCustomer>();services.AddScoped<ICurrentCustomer,TestCustomer>();
   services.RemoveAll<IVehicleLookup>();services.AddSingleton<IVehicleLookup>(Lookup);
   services.RemoveAll<ICompanyProfileClient>();services.AddSingleton<ICompanyProfileClient>(Company);
   services.RemoveAll<ICouncilClient>();services.AddSingleton<ICouncilClient>(Council);
   services.RemoveAll<IMotHistoryClient>();services.AddSingleton<IMotHistoryClient>(Mot);
   TestAuthentication.Configure(services);
  });
 }
 public HttpClient Client() {
  var client=CreateClient(new(){BaseAddress=new Uri("https://localhost")});
  using var scope=Services.CreateScope();scope.ServiceProvider.GetRequiredService<UserInfoDbContext>().Database.EnsureCreated();
  return client;
 }
 protected override void Dispose(bool disposing) {base.Dispose(disposing);if(disposing)connection.Dispose();}
}
public sealed class VehicleTests {
 [Theory]
 [InlineData("ab12 cde","AB12CDE")]
 [InlineData("A1","A1")]
 public void RegistrationIsNormalized(string input,string expected)=>Assert.Equal(expected,UserVehicle.Normalize(input));
 [Theory]
 [InlineData("")]
 [InlineData("../x")]
 [InlineData("AB123456")]
 public void InvalidRegistrationRejected(string input)=>Assert.Throws<ArgumentException>(()=>UserVehicle.Normalize(input));
 [Fact]
 public async Task AuthenticatedAssetsAreIsolatedIdempotentAndOptional() {
  await using var factory=new VehicleFactory();using var client=factory.Client();
  const string path="/api/assets/me";
  Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);
  client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid(),"wrong"));
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(path)).StatusCode);
  var owner=Guid.NewGuid();client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(owner));
  var empty=await client.GetFromJsonAsync<CustomerAssetsDto>(path);
  Assert.Empty(empty!.Vehicles);Assert.Empty(empty.Companies);Assert.Equal(0,factory.Lookup.Calls);
  var added=await client.PostAsJsonAsync(path+"/vehicles?userId="+Guid.NewGuid(),new AddVehicleRequest("ab12 cde"));
  added.EnsureSuccessStatusCode();var vehicle=(await added.Content.ReadFromJsonAsync<VehicleDto>())!;
  Assert.Equal("Available",vehicle.LookupStatus);Assert.Equal("TEST",vehicle.Details!.Make);
  (await client.PostAsJsonAsync(path+"/vehicles",new AddVehicleRequest("AB12CDE"))).EnsureSuccessStatusCode();
  Assert.Equal(1,factory.Lookup.Calls);Assert.Equal(1,factory.Mot.Calls);
  var motPath=path+"/vehicles/"+vehicle.Id+"/mot";
  var history=await client.GetFromJsonAsync<MotHistoryDto>(motPath);
  Assert.Equal("Available",history!.Status);Assert.Single(history.Tests);
  Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(motPath+"?pageSize=1000")).StatusCode);
  (await client.PostAsJsonAsync(path+"/companies",new AddCompanyRequest("sc123456"))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync(path+"/companies",new AddCompanyRequest("SC123456"))).EnsureSuccessStatusCode();
  var own=await client.GetFromJsonAsync<CustomerAssetsDto>(path);Assert.Single(own!.Vehicles);Assert.Single(own.Companies);
  Assert.Equal(HttpStatusCode.TooManyRequests,(await client.PostAsync(path+"/vehicles/"+vehicle.Id+"/refresh",null)).StatusCode);
  client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid()));
  Assert.Empty((await client.GetFromJsonAsync<CustomerAssetsDto>(path))!.Vehicles);
  Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(path+"/vehicles/"+vehicle.Id+"/mot")).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await client.DeleteAsync(path+"/vehicles/"+vehicle.Id)).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync(path+"/vehicles/"+vehicle.Id+"/refresh",null)).StatusCode);
  client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(owner));
  Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(path+"/vehicles/"+vehicle.Id)).StatusCode);
  Assert.Empty((await client.GetFromJsonAsync<CustomerAssetsDto>(path))!.Vehicles);
 }
 [Fact]
 public async Task ProviderFailureStillPersistsReferenceAndMultipleVehiclesWork() {
  await using var factory=new VehicleFactory();using var client=factory.Client();
  factory.Lookup.Fail=true;client.DefaultRequestHeaders.Authorization=new("Bearer",TestAuthentication.Token(Guid.NewGuid()));
  foreach(var registration in new[]{"AB12CDE","XY12ZZZ"}) {
   var response=await client.PostAsJsonAsync("/api/assets/me/vehicles",new AddVehicleRequest(registration));
   response.EnsureSuccessStatusCode();Assert.Equal("Unavailable",(await response.Content.ReadFromJsonAsync<VehicleDto>())!.LookupStatus);
  }
  Assert.Equal(2,(await client.GetFromJsonAsync<CustomerAssetsDto>("/api/assets/me"))!.Vehicles.Count);
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/assets/me/vehicles",new AddVehicleRequest("../"))).StatusCode);
 }
 [Fact]
 public async Task PersistenceLeasePreventsDuplicateLookupAndDeletedRowsAreNotResurrected() {
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  await using var db=new UserInfoDbContext(new DbContextOptionsBuilder<UserInfoDbContext>().UseSqlite(connection).Options);
  await db.Database.EnsureCreatedAsync();var writer=new AssetWriter(db);
  var now=DateTimeOffset.UtcNow;var vehicle=await writer.AddVehicleAsync(UserVehicle.Create(Guid.NewGuid(),"AB12CDE",now),default);
  Assert.True(await writer.TryBeginLookupAsync(vehicle.UserId,vehicle.Id,now,default));
  Assert.False(await writer.TryBeginLookupAsync(vehicle.UserId,vehicle.Id,now.AddSeconds(1),default));
  vehicle.Apply(new VehicleDetails {RegistrationNumber=vehicle.RegistrationNumber},now.AddSeconds(2));
  Assert.True(await writer.RemoveVehicleAsync(vehicle.UserId,vehicle.Id,default));
  Assert.False(await writer.SaveLookupAsync(vehicle,default));
 }
 [Theory]
 [InlineData(400,"Rejected")]
 [InlineData(403,"ProviderAccessDenied")]
 [InlineData(404,"NotFound")]
 [InlineData(429,"RateLimited")]
 [InlineData(503,"Unavailable")]
 public async Task DvlaErrorsAreSafeStatuses(int status,string expected) {
  using var http=new HttpClient(new ResponseHandler((HttpStatusCode)status,"{}"));
  var lookup=new DvlaVehicleLookup(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new DvlaOptions {Enabled=true}),NullLogger<DvlaVehicleLookup>.Instance);
  Assert.Equal(expected,(await lookup.LookupAsync("AB12CDE",default)).Status);
 }
 [Fact]
 public async Task DvlaUsesPostBodyAndDeserializesOptionalFields() {
  var handler=new ResponseHandler(HttpStatusCode.OK,"""{"registrationNumber":"AB12CDE","motExpiryDate":"2027-03-01","automatedVehicle":false,"monthOfFirstRegistration":"2012-12"}""");
  using var http=new HttpClient(handler);
  var lookup=new DvlaVehicleLookup(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new DvlaOptions {Enabled=true}),NullLogger<DvlaVehicleLookup>.Instance);
  var result=await lookup.LookupAsync("AB12CDE",default);
  Assert.Equal(HttpMethod.Post,handler.Method);
  Assert.DoesNotContain("AB12CDE",handler.Url!);
  Assert.Equal("AB12CDE",JsonDocument.Parse(handler.Body!).RootElement.GetProperty("registrationNumber").GetString());
  Assert.Equal(new DateOnly(2027,3,1),result.Details!.MotExpiryDate);
  Assert.Null(result.Details.EngineCapacity);Assert.False(result.Details.AutomatedVehicle);
  Assert.Equal("2012-12",result.Details.MonthOfFirstRegistration);
 }
 [Fact]
 public async Task DisabledLookupMakesNoExternalRequest() {
  using var http=new HttpClient(new ResponseHandler(HttpStatusCode.OK,"{}"));
  var lookup=new DvlaVehicleLookup(new ApiClient(http,NullLogger<ApiClient>.Instance),Options.Create(new DvlaOptions()),NullLogger<DvlaVehicleLookup>.Instance);
  Assert.Equal("NotConfigured",(await lookup.LookupAsync("AB12CDE",default)).Status);
 }
 private sealed class ResponseHandler(HttpStatusCode status,string json):HttpMessageHandler {
  public string? Body;public string? Url;public HttpMethod? Method;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
   Method=request.Method;Url=request.RequestUri!.ToString();Body=await request.Content!.ReadAsStringAsync(token);
   return new(status){Content=new StringContent(json,Encoding.UTF8,"application/json")};
  }
 }
}
