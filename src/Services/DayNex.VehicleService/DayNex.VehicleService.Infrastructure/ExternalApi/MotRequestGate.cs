namespace DayNex.VehicleService.Infrastructure;

// Conservative per-process pacing. Multiple replicas require a shared provider-wide limiter.
public sealed class MotRequestGate(TimeProvider clock) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _nextUtc;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var delay = _nextUtc - clock.GetUtcNow();
            if (delay > TimeSpan.Zero) await Task.Delay(delay, clock, cancellationToken);
            _nextUtc = clock.GetUtcNow().AddMilliseconds(250);
        }
        finally { _gate.Release(); }
    }

    public void Dispose() => _gate.Dispose();
}

public sealed class MotRateLimitHandler(MotRequestGate gate) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
