using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;
public sealed record AddVehicleRequest(string RegistrationNumber);
public sealed record AddCompanyRequest(string CompanyNumber);
public sealed record VehicleDto(Guid Id, string RegistrationNumber, string LookupStatus, DateTimeOffset? RetrievedUtc, VehicleDetails? Details, MotSummaryDto? Mot = null);
public sealed record CompanyDto(Guid Id, string CompanyNumber, string LookupStatus = "Pending", DateTimeOffset? RetrievedUtc = null, CompanyProfileDto? Profile = null);
public sealed record CustomerAssetsDto(IReadOnlyList<VehicleDto> Vehicles, IReadOnlyList<CompanyDto> Companies, LocationDto? Location = null);
public sealed record VehicleLookupResult(string Status, VehicleDetails? Details = null);
public sealed class AssetNotFoundException : Exception;
public sealed class LookupRateLimitedException : Exception;
