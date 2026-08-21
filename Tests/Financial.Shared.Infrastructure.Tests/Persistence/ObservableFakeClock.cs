using Microsoft.Extensions.Time.Testing;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

/// <summary>
/// A steppable clock that also reports when a timer has been armed on it.
/// <para>
/// <see cref="Persistence.DebouncedJsonStorage"/> queues each debounce cycle onto the thread pool,
/// so the cycle's timer is not registered by the time <c>WriteAsync</c> returns. Advancing before
/// that happens moves the clock past a timer that does not exist yet, and the window is then
/// measured from the new "now" - the wait never elapses and the test times out.
/// </para>
/// <para>
/// <see cref="TimersArmed"/> only ever grows, so a test can wait on it exactly the way it waits on
/// <c>ControllableJsonStorage.WrittenJson</c>: polling a value that cannot go backwards, rather
/// than sampling a state that can move on between the check and the assertion.
/// </para>
/// </summary>
internal sealed class ObservableFakeClock : TimeProvider
{
    private readonly FakeTimeProvider _inner;
    private int _timersArmed;

    internal ObservableFakeClock(DateTimeOffset start) => _inner = new FakeTimeProvider(start);

    /// <summary>How many timers have been created on this clock since it was constructed.</summary>
    internal int TimersArmed => Volatile.Read(ref _timersArmed);

    public override long TimestampFrequency => _inner.TimestampFrequency;

    public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

    internal void Advance(TimeSpan delta) => _inner.Advance(delta);

    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    public override long GetTimestamp() => _inner.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        // Counted after the timer exists: nothing on a fake clock can fire until Advance is called,
        // so a test that sees the count has necessarily seen an armed timer.
        var timer = _inner.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _timersArmed);
        return timer;
    }
}
