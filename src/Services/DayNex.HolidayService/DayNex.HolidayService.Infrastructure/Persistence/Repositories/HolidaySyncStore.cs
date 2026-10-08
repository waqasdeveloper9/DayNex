using System.Data;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayNex.HolidayService.Infrastructure.Persistence.Repositories;

public sealed class HolidaySyncStore(HolidayDbContext context) : IHolidaySyncStore
{
    public async Task<int> SynchronizeAsync(IReadOnlyCollection<BankHoliday> holidays, CancellationToken cancellationToken)
    {
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            // Serializing the small holiday catalogue prevents racing syncs. HTTP fetching happens before this transaction.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = (await context.BankHoliday.ToListAsync(cancellationToken))
                .ToDictionary(h => (h.Region, h.Date, h.Title));
            var inserted = 0;
            foreach (var incoming in holidays)
            {
                var key = (incoming.Region, incoming.Date, incoming.Title);
                if (existing.TryGetValue(key, out var holiday))
                {
                    holiday.Notes = incoming.Notes;
                    holiday.Bunting = incoming.Bunting;
                }
                else
                {
                    context.BankHoliday.Add(incoming);
                    existing.Add(key, incoming);
                    inserted++;
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return inserted;
        });
    }
}
