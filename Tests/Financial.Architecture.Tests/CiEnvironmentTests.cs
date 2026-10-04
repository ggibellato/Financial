using System.Globalization;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class CiEnvironmentTests
{
    private static bool RunningOnCi => Environment.GetEnvironmentVariable("CI") == "true";

    [Fact]
    public void Pinned_OnCi_TimeZoneIsLondon()
    {
        if (!RunningOnCi)
        {
            return;
        }

        TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified))
            .Should().Be(TimeSpan.Zero);
        TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified))
            .Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Pinned_OnCi_CultureIsEnGb()
    {
        if (!RunningOnCi)
        {
            return;
        }

        CultureInfo.CurrentCulture.Name.Should().Be("en-GB");
    }
}
