namespace Financial.TestUtilities;

public static class TestClock
{
    public static readonly DateTimeOffset Midsummer = new(2026, 7, 15, 12, 0, 0, TimeSpan.FromHours(1));

    public static readonly DateTimeOffset EndOfJanuary = new(2026, 1, 31, 23, 59, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset StartOfMarch = new(2026, 3, 1, 0, 30, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset FirstOfJulyJustAfterMidnight = new(2026, 7, 1, 0, 30, 0, TimeSpan.FromHours(1));

    public static FakeTimeProvider At(DateTimeOffset? instant = null) => new(instant ?? Midsummer);
}
