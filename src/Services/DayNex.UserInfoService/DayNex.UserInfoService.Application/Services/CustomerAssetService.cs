using DayNex.Domain.Common.Interface;
using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;

public sealed class CustomerAssetService(
    ICurrentCustomer customer,
    IGenericRepository<UserVehicle> vehicles,
    IGenericRepository<UserCompany> companies,
    IAssetWriter writer,
    VehicleRefreshService refresh,
    TimeProvider clock,
    VehicleMotService mot, CompanyProfileService companyProfiles, UserLocationService locations) : ICustomerAssetService
{
    public async Task<CustomerAssetsDto> GetAsync(CancellationToken cancellationToken)
    {
        var userId = await customer.GetUserIdAsync(cancellationToken);
        // The repositories share a scoped EF context, so database operations stay sequential.
        var vehicleRows = await vehicles.GetAsync(new VehiclesForUser(userId), cancellationToken);
        var companyRows = await companies.GetAsync(new CompaniesForUser(userId), cancellationToken);
        return new CustomerAssetsDto(
            vehicleRows.Select(Map).ToArray(),
            companyRows.Select(CompanyProfileService.Map).ToArray(), await locations.GetAsync(cancellationToken));
    }

    public async Task<VehicleDto> AddVehicleAsync(AddVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = await customer.GetUserIdAsync(cancellationToken);
        var row = await writer.AddVehicleAsync(
            UserVehicle.Create(userId, request.RegistrationNumber, clock.GetUtcNow()), cancellationToken);

        // Persist input before provider calls. Provider failures cannot discard the user's reference.
        if (row.LastAttemptUtc is null)
            return await LookupAsync(row, cancellationToken);

        // Supports existing vehicles created before MOT was introduced without repeating DVLA.
        if (row.MotLastAttemptUtc is null)
            await mot.RefreshAsync(userId, row.Id, row.RegistrationNumber, cancellationToken);
        return await LoadVehicleAsync(userId, row.Id, cancellationToken);
    }

    public async Task<VehicleDto> RefreshAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = await customer.GetUserIdAsync(cancellationToken);
        var row = (await vehicles.GetAsync(new OwnedVehicle(userId, id), cancellationToken))
            .SingleOrDefault() ?? throw new AssetNotFoundException();

        if (row.LastAttemptUtc is { } attempt && clock.GetUtcNow() - attempt < TimeSpan.FromMinutes(5))
            throw new LookupRateLimitedException();

        return await LookupAsync(row, cancellationToken);
    }

    private async Task<VehicleDto> LookupAsync(UserVehicle row, CancellationToken cancellationToken)
    {
        await refresh.RefreshAsync(row, cancellationToken);
        return await LoadVehicleAsync(row.UserId, row.Id, cancellationToken);
    }

    private async Task<VehicleDto> LoadVehicleAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var row = (await vehicles.GetAsync(new OwnedVehicle(userId, id), cancellationToken))
            .SingleOrDefault() ?? throw new AssetNotFoundException();
        return Map(row);
    }

    public async Task<MotHistoryDto> GetMotHistoryAsync(
        Guid id, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await mot.ReadAsync(await customer.GetUserIdAsync(cancellationToken),
            id, page, pageSize, cancellationToken) ?? throw new AssetNotFoundException();
    }

    public async Task<CompanyDto> AddCompanyAsync(AddCompanyRequest request, CancellationToken cancellationToken)
    {
        var row = await writer.AddCompanyAsync(UserCompany.Create(
            await customer.GetUserIdAsync(cancellationToken),
            request.CompanyNumber, clock.GetUtcNow()), cancellationToken);
        return row.LastAttemptUtc is null
            ? await companyProfiles.RefreshAsync(row.UserId, row.Id, false, cancellationToken)
            : await companyProfiles.ReadAsync(row.UserId, row.Id, cancellationToken);
    }

    public async Task<bool> RemoveVehicleAsync(Guid id, CancellationToken cancellationToken) =>
        await writer.RemoveVehicleAsync(await customer.GetUserIdAsync(cancellationToken), id, cancellationToken);

    public async Task<bool> RemoveCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        await writer.RemoveCompanyAsync(await customer.GetUserIdAsync(cancellationToken), id, cancellationToken);

    private static VehicleDto Map(UserVehicle vehicle) => new(
        vehicle.Id, vehicle.RegistrationNumber, vehicle.LookupStatus, vehicle.RetrievedUtc, vehicle.Details,
        new MotSummaryDto(vehicle.MotLookupStatus, vehicle.MotRetrievedUtc, vehicle.MotHistory?.HasOutstandingRecall));
}
