namespace DayNex.VehicleService.Domain.Models;

/// <summary>One durable lease and last completed batch report; contains no provider credentials.</summary>
public sealed class VehicleRefreshJobState
{
    public int Id { get; private set; } = 1;
    public Guid? LeaseId { get; private set; }
    public long LeaseUntilUtcTicks { get; private set; }
    public long? CompletedUtcTicks { get; private set; }
    public int Processed { get; private set; }
    public int Failed { get; private set; }
    public int Skipped { get; private set; }
}

