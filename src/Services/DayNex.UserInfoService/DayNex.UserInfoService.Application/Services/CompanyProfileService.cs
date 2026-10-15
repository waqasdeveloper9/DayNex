using DayNex.UserInfoService.Domain.Models;
namespace DayNex.UserInfoService.Application;
public sealed class CompanyProfileService(ICompanyProfileStore store, ICompanyProfileClient client, TimeProvider clock)
{
    public async Task<CompanyDto> ReadAsync(Guid userId, Guid id, CancellationToken token) =>
        Map(await store.FindAsync(userId, id, token) ?? throw new AssetNotFoundException());
    public async Task<CompanyDto> RefreshAsync(Guid userId, Guid id, bool required, CancellationToken token)
    {
        var row = await store.FindAsync(userId, id, token) ?? throw new AssetNotFoundException();
        var attempt = clock.GetUtcNow();
        if (!await store.TryBeginAsync(userId, id, attempt, token))
        {
            if (required) throw new LookupRateLimitedException();
            return Map(row);
        }
        var result = await client.FetchAsync(row.CompanyNumber, token);
        if (!await store.SaveAsync(userId, id, attempt, result, clock.GetUtcNow(), token)) throw new AssetNotFoundException();
        return await ReadAsync(userId, id, token);
    }
    public static CompanyDto Map(UserCompany row)
    {
        var p = row.Profile; var a = p?.RegisteredOffice;
        return new(row.Id, row.CompanyNumber, row.LookupStatus, row.RetrievedUtc, p is null ? null :
            new(p.CompanyName, p.CompanyStatus, p.CompanyType, p.Jurisdiction, p.DateOfCreation,
                p.AccountsDueOn, p.AccountsOverdue, p.ConfirmationStatementDueOn, p.ConfirmationStatementOverdue,
                a is null ? null : new(a.Premises, a.AddressLine1, a.AddressLine2, a.Locality, a.Region, a.Country, a.PostalCode),
                p.SicCodes.Select(c => c.Code).ToArray(), p.PreviousNames.Select(n => new PreviousCompanyNameDto(n.Name,n.EffectiveFrom,n.CeasedOn)).ToArray()));
    }
}
