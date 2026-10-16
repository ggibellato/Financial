using Financial.Shared.Abstractions.Time;
using FluentAssertions;

namespace Financial.Shared.Abstractions.Tests.Time;

[Trait("Category", "Unit")]
public class TimeProviderExtensionsTests
{
    private static readonly DateTimeOffset LondonMidnightAfterUtcEvening = new(2026, 7, 1, 0, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void GetUtcToday_WhenLocalDateIsAheadOfUtc_ReturnsUtcDate()
    {
        var clock = TestClock.At(LondonMidnightAfterUtcEvening);

        clock.GetUtcToday().Should().Be(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void GetLocalToday_WhenLocalDateIsAheadOfUtc_ReturnsLocalDate()
    {
        var clock = TestClock.At(LondonMidnightAfterUtcEvening);

        clock.GetLocalToday().Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void GetLocalDate_WhenLocalDateIsAheadOfUtc_ReturnsLocalMidnight()
    {
        var clock = TestClock.At(LondonMidnightAfterUtcEvening);

        clock.GetLocalDate().Should().Be(new DateTime(2026, 7, 1));
    }
}
