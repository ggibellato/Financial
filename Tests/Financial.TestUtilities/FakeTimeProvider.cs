namespace Financial.TestUtilities;

public sealed class FakeTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    private readonly TimeZoneInfo _localTimeZone;
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now, TimeZoneInfo? localTimeZone = null)
    {
        _now = now;
        _localTimeZone = localTimeZone ?? London;
    }

    public override TimeZoneInfo LocalTimeZone => _localTimeZone;

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetUtcNow(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan delta) => _now += delta;
}
