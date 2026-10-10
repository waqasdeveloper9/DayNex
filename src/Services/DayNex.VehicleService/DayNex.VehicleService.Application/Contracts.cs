using DayNex.VehicleService.Domain;

namespace DayNex.VehicleService.Application;
public sealed record AddVehicleRequest(string RegistrationNumber);
public sealed record AddCompanyRequest(string CompanyNumber);
public sealed record VehicleDto(Guid Id, string RegistrationNumber, string LookupStatus, DateTimeOffset? RetrievedUtc, VehicleDetails? Details);
public sealed record CompanyDto(Guid Id, string CompanyNumber);
public sealed record CustomerAssetsDto(IReadOnlyList<VehicleDto> Vehicles, IReadOnlyList<CompanyDto> Companies);
public sealed record VehicleLookupResult(string Status, VehicleDetails? Details = null);
public interface IVehicleLookup
{
    Task<VehicleLookupResult> LookupAsync(string registration, CancellationToken token);
}

public interface ICurrentCustomer
{
    Task<Guid> GetUserIdAsync(CancellationToken token);
}

public interface IAssetWriter
{
    Task<UserVehicle> AddVehicleAsync(UserVehicle value, CancellationToken token);
    Task<UserCompany> AddCompanyAsync(UserCompany value, CancellationToken token);
    Task<bool> TryBeginLookupAsync(Guid userId, Guid id, DateTimeOffset now, CancellationToken token);
    Task<bool> SaveLookupAsync(UserVehicle value, CancellationToken token);
    Task<bool> RemoveVehicleAsync(Guid userId, Guid id, CancellationToken token);
    Task<bool> RemoveCompanyAsync(Guid userId, Guid id, CancellationToken token);
}

public sealed class AssetNotFoundException : Exception;
public sealed class LookupRateLimitedException : Exception;
