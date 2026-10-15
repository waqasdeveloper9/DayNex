using System.Data;
using DayNex.UserInfoService.Application;
using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace DayNex.UserInfoService.Infrastructure;
public sealed class UserLocationStore(UserInfoDbContext db) : IUserLocationStore
{
    public Task<UserLocation?> FindAsync(Guid userId,CancellationToken token)=>db.Set<UserLocation>().AsNoTracking().Include(x=>x.Council).SingleOrDefaultAsync(x=>x.Id==userId,token);
    public Task<UserLocation> SetPostcodeAsync(Guid userId,string postcode,DateTimeOffset now,CancellationToken token)=>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async()=> {
            if(userId==Guid.Empty) throw new ArgumentException("A user is required.");
            postcode=UserLocation.Normalize(postcode);
            db.ChangeTracker.Clear();
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var row=await db.Set<UserLocation>().Include(x=>x.Council).SingleOrDefaultAsync(x=>x.Id==userId,token);
            if(row is null) {row=new UserLocation {Id=userId,Postcode=postcode,UpdatedUtc=now,LookupStatus=postcode.Length==0?"NotProvided":"Pending"};db.Add(row);}
            else if(row.Postcode!=postcode) {
                // Invalidate old council before making an external request for a new postcode.
                if(row.Council is {} council) db.Remove(council);
                row.Council=null;row.Postcode=postcode;row.Revision=Guid.NewGuid();row.LastAttemptUtcTicks=null;
                row.RetrievedUtc=null;row.LookupStatus=postcode.Length==0?"NotProvided":"Pending";row.UpdatedUtc=now;
            }
            await db.SaveChangesAsync(token);await tx.CommitAsync(token);return row;
        });
    public async Task<bool> TryBeginAsync(Guid userId,Guid revision,long attempt,CancellationToken token) {
        var cutoff=attempt-TimeSpan.FromMinutes(5).Ticks;
        return await db.Set<UserLocation>().Where(x=>x.Id==userId && x.Revision==revision && (x.LastAttemptUtcTicks==null || x.LastAttemptUtcTicks<=cutoff))
            .ExecuteUpdateAsync(u=>u.SetProperty(x=>x.LastAttemptUtcTicks,attempt).SetProperty(x=>x.LookupStatus,"Pending"),token)>0;
    }
    public Task<bool> SaveAsync(Guid userId,Guid revision,long attempt,CouncilLookupResult result,DateTimeOffset now,CancellationToken token)=>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async()=> {
            db.ChangeTracker.Clear();
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var row=await db.Set<UserLocation>().Include(x=>x.Council).SingleOrDefaultAsync(x=>x.Id==userId,token);
            if(row is null || row.Revision!=revision || row.LastAttemptUtcTicks!=attempt) return false;
            if(result.Status=="Available" && result.Council is {} council) {
                council.UserId=userId;
                if(row.Council is null) row.Council=council;
                else db.Entry(row.Council).CurrentValues.SetValues(council);
                row.RetrievedUtc=now;
            }
            row.LookupStatus=result.Status;row.UpdatedUtc=now;
            await db.SaveChangesAsync(token);await tx.CommitAsync(token);return true;
        });
}
