using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DividendValuationRulesTests
{
    [Fact]
    public void CalculatePriceMaxBuy_WhenAverageDividendZero_ReturnsZero()
    {
        DividendValuationRules.CalculatePriceMaxBuy(0m).Should().Be(0m);
    }

    [Fact]
    public void CalculatePriceMaxBuy_WhenAverageDividendPositive_DividesByRequiredYield()
    {
        var result = DividendValuationRules.CalculatePriceMaxBuy(6m);

        result.Should().Be(6m / DividendValuationRules.RequiredYield);
    }

    [Fact]
    public void CalculateDiscountPercent_WhenPriceMaxBuyZero_ReturnsZero()
    {
        DividendValuationRules.CalculateDiscountPercent(price: 10m, priceMaxBuy: 0m).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscountPercent_WhenPriceBelowPriceMaxBuy_ReturnsPositivePercent()
    {
        var result = DividendValuationRules.CalculateDiscountPercent(price: 80m, priceMaxBuy: 100m);

        result.Should().Be(20m);
    }

    [Fact]
    public void CalculateDividendYieldPercent_WhenPriceZero_ReturnsZero()
    {
        DividendValuationRules.CalculateDividendYieldPercent(averageDividend: 5m, price: 0m).Should().Be(0m);
    }

    [Fact]
    public void CalculateDividendYieldPercent_WhenPricePositive_ReturnsPercent()
    {
        var result = DividendValuationRules.CalculateDividendYieldPercent(averageDividend: 5m, price: 100m);

        result.Should().Be(5m);
    }
}
