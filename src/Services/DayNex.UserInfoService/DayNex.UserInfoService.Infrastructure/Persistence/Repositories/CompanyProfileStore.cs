using System.Data;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace DayNex.UserInfoService.Infrastructure;
public sealed class CompanyProfileStore(UserInfoDbContext db) : ICompanyProfileStore
{
    public Task<UserCompany?> FindAsync(Guid userId,Guid companyId,CancellationToken token)=>db.Companies.AsNoTracking()
        .Include(x=>x.Profile!).ThenInclude(x=>x.RegisteredOffice)
        .Include(x=>x.Profile!).ThenInclude(x=>x.SicCodes)
        .Include(x=>x.Profile!).ThenInclude(x=>x.PreviousNames)
        .AsSplitQuery().SingleOrDefaultAsync(x=>x.Id==companyId && x.UserId==userId,token);
    public async Task<bool> TryBeginAsync(Guid userId,Guid companyId,DateTimeOffset now,CancellationToken token)
    {
        var cutoff=now-TimeSpan.FromMinutes(5);
        return await db.Companies.Where(x=>x.UserId==userId && x.Id==companyId && (x.LastAttemptUtc==null || x.LastAttemptUtc<=cutoff))
            .ExecuteUpdateAsync(u=>u.SetProperty(x=>x.LastAttemptUtc,now).SetProperty(x=>x.LookupStatus,"Pending"),token)>0;
    }
    public Task<bool> SaveAsync(Guid userId,Guid companyId,DateTimeOffset attempt,CompanyLookupResult result,DateTimeOffset now,CancellationToken token)=>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async()=> {
            db.ChangeTracker.Clear();
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var row=await db.Companies.SingleOrDefaultAsync(x=>x.Id==companyId && x.UserId==userId,token);
            if(row is null) return false;
            if(row.LastAttemptUtc!=attempt) return true; // Ignore an expired lease's response.
            if(result.Profile is { } profile && result.Status=="Available") {
                if(profile.CompanyNumber!=row.CompanyNumber) throw new ArgumentException("Company snapshot mismatch.");
                var existing=await db.CompanyProfiles.SingleOrDefaultAsync(x=>x.Id==companyId,token);
                if(existing is not null) {db.Remove(existing);await db.SaveChangesAsync(token);}
                profile.Id=companyId;profile.RetrievedUtc=now;
                if(profile.RegisteredOffice is {} office) office.CompanyProfileId=companyId;
                foreach(var code in profile.SicCodes) code.CompanyProfileId=companyId;
                foreach(var name in profile.PreviousNames) name.CompanyProfileId=companyId;
                db.CompanyProfiles.Add(profile);
                row.RecordLookup(result.Status,attempt,now);
            }
            else row.RecordLookup(result.Status,attempt,null); // Preserve the last successful snapshot.
            await db.SaveChangesAsync(token);await tx.CommitAsync(token);return true;
        });
}
