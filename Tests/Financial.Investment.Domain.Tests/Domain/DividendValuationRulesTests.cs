using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DividendValuationRulesTests
{
    [Fact]
    public void RequiredYield_IsSixPercent()
    {
        DividendValuationRules.RequiredYield.Should().Be(0.06m);
    }

    [Fact]
    public void DividendYearsLookback_IsFiveYears()
    {
        DividendValuationRules.DividendYearsLookback.Should().Be(5);
    }
}
