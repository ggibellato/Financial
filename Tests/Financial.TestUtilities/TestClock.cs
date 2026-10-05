using Microsoft.Extensions.Time.Testing;

namespace Financial.TestUtilities;

public static class TestClock
{
    private static readonly Lazy<TimeZoneInfo> London = new(() => TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));

    public static readonly DateTimeOffset Default = new(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(1));

    public static readonly DateTimeOffset EndOfJanuary = new(2026, 1, 31, 23, 59, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset StartOfMarch = new(2026, 3, 1, 0, 30, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset FirstOfJulyJustAfterMidnight = new(2026, 7, 1, 0, 30, 0, TimeSpan.FromHours(1));

    public static readonly DateOnly Today = DateOnly.FromDateTime(Default.DateTime);

    public static readonly DateTime LocalToday = Today.ToDateTime(TimeOnly.MinValue);

    public static FakeTimeProvider At(DateTimeOffset? instant = null)
    {
        var clock = new FakeTimeProvider(instant ?? Default);
        clock.SetLocalTimeZone(London.Value);
        return clock;
    }
}
