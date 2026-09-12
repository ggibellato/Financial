using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class HoldingValuationCalculatorTests
{
    [Fact]
    public void Calculate_NoPrice_MarketValueAndUnrealisedGainAndPriceAsOfDateAreAllNull()
    {
        var result = HoldingValuationCalculator.Calculate(10m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.MarketValue.Should().BeNull();
        result.UnrealisedGain.Should().BeNull();
        result.PriceAsOfDate.Should().BeNull();
    }

    [Fact]
    public void Calculate_WithPrice_MarketValueAndUnrealisedGainAndPriceAsOfDateAreAllPopulated()
    {
        var price = CreatePrice(new DateOnly(2026, 9, 10), 8m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, new DateOnly(2026, 9, 10));

        result.MarketValue.Should().Be(80m);
        result.UnrealisedGain.Should().Be(30m);
        result.PriceAsOfDate.Should().Be(new DateOnly(2026, 9, 10));
    }

    [Fact]
    public void Calculate_CostOfUnitsHeld_UsesOpenPositionCostCalculator()
    {
        var price = CreatePrice(new DateOnly(2026, 9, 10), 8m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, new DateOnly(2026, 9, 10));

        result.CostOfUnitsHeld.Should().Be(OpenPositionCostCalculator.CostOfUnitsHeld(10m, 5m));
    }

    [Fact]
    public void Calculate_UnrealisedGain_IsMarketValueMinusCostOfUnitsHeld()
    {
        var price = CreatePrice(new DateOnly(2026, 9, 10), 3m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, new DateOnly(2026, 9, 10));

        result.MarketValue.Should().Be(30m);
        result.UnrealisedGain.Should().Be(result.MarketValue!.Value - result.CostOfUnitsHeld);
        result.UnrealisedGain.Should().Be(-20m);
    }

    [Fact]
    public void Calculate_PriceDatedTheValuationDate_IsNotStale()
    {
        var valuationDate = new DateOnly(2026, 8, 31); // Monday
        var price = CreatePrice(valuationDate, 8m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, valuationDate);

        result.IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void Calculate_FridayPriceReadOnMonday_IsCurrent()
    {
        var friday = new DateOnly(2026, 8, 28);
        var monday = new DateOnly(2026, 8, 31);
        var price = CreatePrice(friday, 8m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, monday);

        result.IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void Calculate_FridayPriceReadOnTuesday_IsStale()
    {
        var friday = new DateOnly(2026, 8, 28);
        var tuesday = new DateOnly(2026, 9, 1);
        var price = CreatePrice(friday, 8m);

        var result = HoldingValuationCalculator.Calculate(10m, 5m, price, tuesday);

        result.IsPriceStale.Should().BeTrue();
    }

    [Fact]
    public void Calculate_NoPrice_PriceOnlyReturnAndTotalReturnDefaultNull()
    {
        var result = HoldingValuationCalculator.Calculate(10m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.PriceOnlyReturn.Should().BeNull();
        result.TotalReturn.Should().BeNull();
    }

    [Fact]
    public void Calculate_ZeroQuantityAndNoPrice_MarketValueIsZero_NotUnavailable()
    {
        var result = HoldingValuationCalculator.Calculate(0m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.MarketValue.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ZeroQuantityAndNoPrice_UnrealisedGainIsZero()
    {
        var result = HoldingValuationCalculator.Calculate(0m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.UnrealisedGain.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ZeroQuantityAndNoPrice_ReportsNoPriceAsOfDateAndIsNeverStale()
    {
        var result = HoldingValuationCalculator.Calculate(0m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.PriceAsOfDate.Should().BeNull();
        result.IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void Calculate_NonZeroQuantityAndNoPrice_StillReportsUnavailable()
    {
        var result = HoldingValuationCalculator.Calculate(10m, 5m, price: null, new DateOnly(2026, 9, 10));

        result.MarketValue.Should().BeNull();
    }

    [Fact]
    public void NotMarkedToMarket_MarketValueIsZero_NotNull()
    {
        var result = HoldingValuationCalculator.NotMarkedToMarket(28m, 71.5m);

        result.MarketValue.Should().Be(0m);
    }

    [Fact]
    public void NotMarkedToMarket_NeverReportsUnrealisedGain()
    {
        var result = HoldingValuationCalculator.NotMarkedToMarket(28m, 71.5m);

        result.UnrealisedGain.Should().BeNull();
    }

    [Fact]
    public void NotMarkedToMarket_ReportsNoPriceAsOfDateAndIsNeverStale()
    {
        var result = HoldingValuationCalculator.NotMarkedToMarket(28m, 71.5m);

        result.PriceAsOfDate.Should().BeNull();
        result.IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void NotMarkedToMarket_StillReportsCostOfUnitsHeld()
    {
        var result = HoldingValuationCalculator.NotMarkedToMarket(28m, 71.5m);

        result.CostOfUnitsHeld.Should().Be(OpenPositionCostCalculator.CostOfUnitsHeld(28m, 71.5m));
    }

    private static AssetPriceSnapshot CreatePrice(DateOnly date, decimal price) =>
        AssetPriceSnapshot.Create(date, price, ValuationMethod.MarketPrice, PriceSource.Unknown, currency: string.Empty, sourceReference: null, DateTimeOffset.UtcNow);
}
