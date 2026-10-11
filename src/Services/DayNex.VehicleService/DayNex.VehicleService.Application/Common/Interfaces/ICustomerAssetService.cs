namespace DayNex.VehicleService.Application;
public interface ICustomerAssetService
{
    Task<CustomerAssetsDto> GetAsync(CancellationToken cancellationToken);
    Task<VehicleDto> AddVehicleAsync(AddVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleDto> RefreshAsync(Guid id, CancellationToken cancellationToken);
    Task<CompanyDto> AddCompanyAsync(AddCompanyRequest request, CancellationToken cancellationToken);
    Task<bool> RemoveVehicleAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> RemoveCompanyAsync(Guid id, CancellationToken cancellationToken);
}
