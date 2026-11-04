using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class PotAsOfDateResolverTests
{
    [Fact]
    public void LastDayOfPriorMonth_MidYear_ReturnsLastDayOfPreviousMonth()
    {
        var result = PotAsOfDateResolver.LastDayOfPriorMonth(2026, 8);

        result.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void LastDayOfPriorMonth_January_ReturnsDecember31OfPriorYear()
    {
        var result = PotAsOfDateResolver.LastDayOfPriorMonth(2026, 1);

        result.Should().Be(new DateOnly(2025, 12, 31));
    }
}
