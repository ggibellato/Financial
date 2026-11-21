using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class DividendValuationRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1.2)]
    public void CalculatePriceMaxBuy_WhenAverageDividendNotPositive_ReturnsNull(decimal averageDividend)
    {
        DividendValuationRules.CalculatePriceMaxBuy(averageDividend).Should().BeNull();
    }

    [Fact]
    public void CalculatePriceMaxBuy_WhenAverageDividendPositive_DividesByRequiredYield()
    {
        var result = DividendValuationRules.CalculatePriceMaxBuy(6m);

        result.Should().Be(6m / DividendValuationRules.RequiredYield);
    }

    [Fact]
    public void CalculateDiscountPercent_WhenNoPriceMaxBuy_ReturnsNull()
    {
        DividendValuationRules.CalculateDiscountPercent(price: 10m, priceMaxBuy: null).Should().BeNull();
    }

    [Fact]
    public void CalculateDiscountPercent_WhenPriceZero_ReturnsNull()
    {
        DividendValuationRules.CalculateDiscountPercent(price: 0m, priceMaxBuy: 100m).Should().BeNull();
    }

    [Fact]
    public void CalculateDiscountPercent_WhenPriceBelowPriceMaxBuy_ReturnsPositivePercent()
    {
        var result = DividendValuationRules.CalculateDiscountPercent(price: 80m, priceMaxBuy: 100m);

        result.Should().Be(20m);
    }

    [Fact]
    public void CalculateDividendYieldPercent_WhenPriceZero_ReturnsNull()
    {
        DividendValuationRules.CalculateDividendYieldPercent(averageDividend: 5m, price: 0m).Should().BeNull();
    }

    [Fact]
    public void CalculateDividendYieldPercent_WhenPricePositive_ReturnsPercent()
    {
        var result = DividendValuationRules.CalculateDividendYieldPercent(averageDividend: 5m, price: 100m);

        result.Should().Be(5m);
    }
}
