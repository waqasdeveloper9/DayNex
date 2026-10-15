using DayNex.UserInfoService.Domain.Models;
namespace DayNex.UserInfoService.Application;
public sealed record CompanyLookupResult(string Status, CompanyProfile? Profile = null);
public interface ICompanyProfileClient
{
    Task<CompanyLookupResult> FetchAsync(string number, CancellationToken token);
}
public interface ICompanyProfileStore
{
    Task<UserCompany?> FindAsync(Guid userId, Guid companyId, CancellationToken token);
    Task<bool> TryBeginAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken token);
    Task<bool> SaveAsync(Guid userId, Guid companyId, DateTimeOffset attempt, CompanyLookupResult result, DateTimeOffset now, CancellationToken token);
}
