using Microsoft.Extensions.Logging;
namespace DayNex.VehicleService.Application;

public sealed class VehicleRefreshJob(IVehicleRefreshJobStore store, VehicleRefreshService refresh,
    TimeProvider clock, ILogger<VehicleRefreshJob> logger)
{
    public async Task<VehicleRefreshBatchResult> RunBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(batchSize));
        var batchId = Guid.NewGuid();
        if (!await store.AcquireAsync(batchId, clock.GetUtcNow(), cancellationToken))
            throw new RefreshJobBusyException();
        var processed = 0; var failed = 0; var skipped = 0;
        try
        {
            var rows = await store.ReadDueAsync(clock.GetUtcNow(), batchSize, cancellationToken);
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await store.MarkAttemptAsync(batchId, row.Id, clock.GetUtcNow(), cancellationToken))
                    throw new RefreshJobBusyException();
                try
                {
                    var succeeded = await refresh.RefreshAsync(row, cancellationToken);
                    processed++;
                    if (!succeeded) failed++;
                }
                catch (AssetNotFoundException) { skipped++; }
                catch (LookupRateLimitedException) { skipped++; }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    failed++;
                    logger.LogWarning("Vehicle refresh batch {BatchId} encountered {ErrorType}.",
                        batchId, exception.GetType().Name);
                }
            }
            return new(batchId, processed, failed, skipped,
                await store.HasMoreAsync(clock.GetUtcNow(), cancellationToken), clock.GetUtcNow());
        }
        finally
        {
            // Release even if the caller disconnects. A crashed process is recovered by lease expiry.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await store.CompleteAsync(new(batchId, processed, failed, skipped, false, clock.GetUtcNow()), cleanup.Token);
            logger.LogInformation("Vehicle refresh batch {BatchId}: processed {Processed}, failed {Failed}, skipped {Skipped}.",
                batchId, processed, failed, skipped);
        }
    }
}

