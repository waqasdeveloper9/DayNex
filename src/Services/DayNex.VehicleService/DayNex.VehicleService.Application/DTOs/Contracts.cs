using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;
public sealed record AddVehicleRequest(string RegistrationNumber);
public sealed record AddCompanyRequest(string CompanyNumber);
public sealed record VehicleDto(Guid Id, string RegistrationNumber, string LookupStatus, DateTimeOffset? RetrievedUtc, VehicleDetails? Details);
public sealed record CompanyDto(Guid Id, string CompanyNumber);
public sealed record CustomerAssetsDto(IReadOnlyList<VehicleDto> Vehicles, IReadOnlyList<CompanyDto> Companies);
public sealed record VehicleLookupResult(string Status, VehicleDetails? Details = null);
public sealed class AssetNotFoundException : Exception;
public sealed class LookupRateLimitedException : Exception;
