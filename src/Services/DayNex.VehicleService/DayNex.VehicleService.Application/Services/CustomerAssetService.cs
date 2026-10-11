using DayNex.Domain.Common.Interface;
using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;
public sealed class CustomerAssetService(ICurrentCustomer customer, IGenericRepository<UserVehicle> vehicles, IGenericRepository<UserCompany> companies, IAssetWriter writer, IVehicleLookup lookup, TimeProvider clock) : ICustomerAssetService
{
    public async Task<CustomerAssetsDto> GetAsync(CancellationToken token)
    {
        var userId = await customer.GetUserIdAsync(token);
        // Same scoped EF context: queries must remain sequential.
        var vehicleRows = await vehicles.GetAsync(new VehiclesForUser(userId), token);
        var companyRows = await companies.GetAsync(new CompaniesForUser(userId), token);
        return new(vehicleRows.Select(Map).ToArray(), companyRows.Select(c => new CompanyDto(c.Id, c.CompanyNumber)).ToArray());
    }

    public async Task<VehicleDto> AddVehicleAsync(AddVehicleRequest request, CancellationToken token)
    {
        var userId = await customer.GetUserIdAsync(token);
        var row = await writer.AddVehicleAsync(UserVehicle.Create(userId, request.RegistrationNumber, clock.GetUtcNow()), token);
        // Saving the reference first means a provider outage does not lose the user's input.
        if (row.LastAttemptUtc is null)
            return await LookupAsync(row, token);
        return Map(row);
    }

    public async Task<VehicleDto> RefreshAsync(Guid id, CancellationToken token)
    {
        var userId = await customer.GetUserIdAsync(token);
        var row = (await vehicles.GetAsync(new OwnedVehicle(userId, id), token)).SingleOrDefault() ?? throw new AssetNotFoundException();
        if (row.LastAttemptUtc is { } attempt && clock.GetUtcNow() - attempt < TimeSpan.FromMinutes(5))
            throw new LookupRateLimitedException();
        return await LookupAsync(row, token);
    }

    private async Task<VehicleDto> LookupAsync(UserVehicle row, CancellationToken token)
    {
        if (!await writer.TryBeginLookupAsync(row.UserId, row.Id, clock.GetUtcNow(), token))
            throw new LookupRateLimitedException();
        var result = await lookup.LookupAsync(row.RegistrationNumber, token);
        if (result.Details is not null)
            row.Apply(result.Details, clock.GetUtcNow());
        else
            row.RecordAttempt(result.Status, clock.GetUtcNow());
        if (!await writer.SaveLookupAsync(row, token))
            throw new AssetNotFoundException();
        return Map(row);
    }

    public async Task<CompanyDto> AddCompanyAsync(AddCompanyRequest request, CancellationToken token)
    {
        var row = await writer.AddCompanyAsync(UserCompany.Create(await customer.GetUserIdAsync(token), request.CompanyNumber, clock.GetUtcNow()), token);
        return new(row.Id, row.CompanyNumber);
    }

    public async Task<bool> RemoveVehicleAsync(Guid id, CancellationToken token) => await writer.RemoveVehicleAsync(await customer.GetUserIdAsync(token), id, token);
    public async Task<bool> RemoveCompanyAsync(Guid id, CancellationToken token) => await writer.RemoveCompanyAsync(await customer.GetUserIdAsync(token), id, token);
    private static VehicleDto Map(UserVehicle v) => new(v.Id, v.RegistrationNumber, v.LookupStatus, v.RetrievedUtc, v.Details);
}
