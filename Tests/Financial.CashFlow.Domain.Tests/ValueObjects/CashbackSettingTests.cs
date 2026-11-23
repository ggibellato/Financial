using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class CashbackSettingTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(2)]
    [InlineData(100)]
    public void On_WithinRange_IsEnabledWithTheRate(decimal ratePercent)
    {
        var cashback = CashbackSetting.On(ratePercent);

        using (new AssertionScope())
        {
            cashback.Enabled.Should().BeTrue();
            cashback.RatePercent.Should().Be(ratePercent);
        }
    }

    [Theory]
    [Trait("AC", "P65-F01-cashback-setting-02")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void On_OutsideRange_Throws(decimal ratePercent)
    {
        var act = () => CashbackSetting.On(ratePercent);

        act.Should().Throw<ArgumentException>().WithMessage("Cashback rate must be more than 0% and at most 100%.");
    }

    [Fact]
    [Trait("AC", "P65-F01-cashback-setting-02")]
    public void On_WithThreeDecimalPlaces_Throws()
    {
        var act = () => CashbackSetting.On(1.125m);

        act.Should().Throw<ArgumentException>().WithMessage("Cashback rate can have at most 2 decimal places.");
    }

    [Fact]
    public void Off_HasNoRate()
    {
        using (new AssertionScope())
        {
            CashbackSetting.Off.Enabled.Should().BeFalse();
            CashbackSetting.Off.RatePercent.Should().BeNull();
        }
    }

    [Fact]
    public void From_EnabledWithRate_IsOn()
    {
        CashbackSetting.From(true, 1.5m).Should().Be(CashbackSetting.On(1.5m));
    }

    [Fact]
    public void From_DisabledWithoutRate_IsOff()
    {
        CashbackSetting.From(false, null).Should().Be(CashbackSetting.Off);
    }

    [Fact]
    [Trait("AC", "P65-F01-cashback-setting-02")]
    public void From_EnabledWithoutRate_Throws()
    {
        var act = () => CashbackSetting.From(true, null);

        act.Should().Throw<ArgumentException>().WithMessage("Enter the cashback rate.");
    }

    [Fact]
    public void From_DisabledWithRate_Throws()
    {
        var act = () => CashbackSetting.From(false, 2m);

        act.Should().Throw<ArgumentException>().WithMessage("A cashback rate can only be set while cashback is on.");
    }

    [Theory]
    [Trait("AC", "P65-F02-cashback-expenses-01")]
    [InlineData(2, 12.50, 0.25)]
    [InlineData(2, 12.99, 0.25)]
    [InlineData(1.5, 9.99, 0.14)]
    [InlineData(1.5, 0.66, 0.00)]
    public void SuggestionFor_IsRateTimesValueRoundedDownToThePenny(decimal ratePercent, decimal value, decimal expected)
    {
        CashbackSetting.On(ratePercent).SuggestionFor(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void SuggestionFor_NonPositiveValue_IsZero(decimal value)
    {
        CashbackSetting.On(2m).SuggestionFor(value).Should().Be(0m);
    }
}
