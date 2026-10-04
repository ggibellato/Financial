using System.Globalization;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class CiEnvironmentTests
{
    [Fact]
    public void Pinned_OnCi_TimeZoneIsLondonAndCultureIsEnGb()
    {
        if (Environment.GetEnvironmentVariable("CI") != "true")
        {
            return;
        }

        TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified))
            .Should().Be(TimeSpan.Zero);
        TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified))
            .Should().Be(TimeSpan.FromHours(1));
        CultureInfo.CurrentCulture.Name.Should().Be("en-GB");
    }
}
