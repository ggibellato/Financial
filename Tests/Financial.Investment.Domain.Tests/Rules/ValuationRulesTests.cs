using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class ValuationRulesTests
{
    [Theory]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public void For_ProviderValueAndManual_ArePositionTotal(ValuationMethod method)
    {
        ValuationRules.For(method).Should().BeSameAs(ValuationRules.PositionTotal);
    }

    [Theory]
    [InlineData(ValuationMethod.Unspecified)]
    [InlineData(ValuationMethod.MarketPrice)]
    [InlineData(ValuationMethod.BondQuote)]
    public void For_OtherMethods_ArePerUnit(ValuationMethod method)
    {
        ValuationRules.For(method).Should().BeSameAs(ValuationRules.PerUnit);
    }

    [Fact]
    public void AllowsZeroPrice_OnlyForPositionTotal()
    {
        using (new AssertionScope())
        {
            ValuationRules.PositionTotal.AllowsZeroPrice.Should().BeTrue();
            ValuationRules.PerUnit.AllowsZeroPrice.Should().BeFalse();
        }
    }

    [Fact]
    public void ValueAt_PerUnitMultipliesAndPositionTotalTakesTheRecordedFigure()
    {
        var price = Snapshot(25m);

        using (new AssertionScope())
        {
            ValuationRules.PerUnit.ValueAt(4m, price).Should().Be(100m);
            ValuationRules.PositionTotal.ValueAt(4m, price).Should().Be(25m);
        }
    }

    [Fact]
    public void ValueOfPortion_PerUnitMultipliesAndPositionTotalProRates()
    {
        var price = Snapshot(1000m);

        using (new AssertionScope())
        {
            ValuationRules.PerUnit.ValueOfPortion(3m, 4m, price).Should().Be(3000m);
            ValuationRules.PositionTotal.ValueOfPortion(3m, 4m, price).Should().Be(750m);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, null)]
    public void ValueWithoutPrice_PerUnit_IsZeroOnlyForAnEmptyPosition(int quantity, int? expected)
    {
        ValuationRules.PerUnit.ValueWithoutPrice(quantity).Should().Be(expected);
    }

    [Fact]
    public void ValueWithoutPrice_PositionTotal_IsUnavailable()
    {
        ValuationRules.PositionTotal.ValueWithoutPrice(5m).Should().BeNull();
    }

    private static AssetPriceSnapshot Snapshot(decimal price) =>
        AssetPriceSnapshot.Create(TestClock.Today, price, ValuationMethod.MarketPrice, PriceSource.Unknown, string.Empty, null, TestClock.Default, TestClock.Today);
}
