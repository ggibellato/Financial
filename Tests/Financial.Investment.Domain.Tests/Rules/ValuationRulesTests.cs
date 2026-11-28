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
    public void ForBalanceHoldings_OnlyForPositionTotal()
    {
        using (new AssertionScope())
        {
            ValuationRules.PositionTotal.ForBalanceHoldings.Should().BeTrue();
            ValuationRules.PerUnit.ForBalanceHoldings.Should().BeFalse();
        }
    }

    [Fact]
    public void PositionTotal_IsNeverFetchedAndRecordsProviderValuation()
    {
        using (new AssertionScope())
        {
            ValuationRules.PositionTotal.FetchesAutomatically.Should().BeFalse();
            ValuationRules.PositionTotal.ManualEntrySource.Should().Be(PriceSource.ProviderValuation);
            ValuationRules.PerUnit.FetchesAutomatically.Should().BeTrue();
            ValuationRules.PerUnit.ManualEntrySource.Should().Be(PriceSource.Manual);
        }
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

    private static AssetPriceSnapshot Snapshot(decimal price) =>
        AssetPriceSnapshot.Create(TestClock.Today, price, ValuationMethod.MarketPrice, PriceSource.Unknown, string.Empty, null, TestClock.Default, TestClock.Today);
}
