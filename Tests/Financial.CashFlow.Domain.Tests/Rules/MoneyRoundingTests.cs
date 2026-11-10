using System.Globalization;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class MoneyRoundingTests
{
    [Theory]
    [InlineData("2.344", "2.34")]
    [InlineData("2.346", "2.35")]
    [InlineData("0.125", "0.13")]
    [InlineData("1.005", "1.01")]
    [InlineData("-0.125", "-0.13")]
    [InlineData("-2.344", "-2.34")]
    [InlineData("80", "80")]
    [InlineData("0.004", "0")]
    public void ToCents_RoundsToTwoDecimalsWithMidpointsAwayFromZero(string value, string expected)
    {
        MoneyRounding.ToCents(decimal.Parse(value, CultureInfo.InvariantCulture))
            .Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }
}
