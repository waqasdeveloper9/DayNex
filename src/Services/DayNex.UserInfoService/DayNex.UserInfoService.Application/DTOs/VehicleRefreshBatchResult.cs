namespace DayNex.UserInfoService.Application;
public sealed record VehicleRefreshBatchResult(Guid BatchId, int Processed, int Failed,
    int Skipped, bool HasMore, DateTimeOffset CompletedUtc);
public sealed class RefreshJobBusyException : Exception;

