using Financial.Domain.Rules;
using FluentAssertions;

namespace Financial.Domain.Tests;

public class ProfitCalculatorTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    [InlineData(-5, 10)]
    public void HasCostBasis_WhenAveragePriceOrQuantityNotPositive_ReturnsFalse(decimal averagePrice, decimal quantity)
    {
        ProfitCalculator.HasCostBasis(averagePrice, quantity).Should().BeFalse();
    }

    [Fact]
    public void HasCostBasis_WhenBothPositive_ReturnsTrue()
    {
        ProfitCalculator.HasCostBasis(10m, 5m).Should().BeTrue();
    }

    [Fact]
    public void CalculateResultFraction_WhenNoCostBasis_ReturnsZero()
    {
        ProfitCalculator.CalculateResultFraction(0m, 10m, 500m).Should().Be(0m);
    }

    [Fact]
    public void CalculateResultFraction_WhenCurrentValueEqualsCostBasis_ReturnsZero()
    {
        var result = ProfitCalculator.CalculateResultFraction(10m, 5m, 50m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateResultFraction_WhenCurrentValueDoubled_ReturnsOne()
    {
        var result = ProfitCalculator.CalculateResultFraction(10m, 5m, 100m);

        result.Should().Be(1m);
    }

    [Fact]
    public void CalculateResultFraction_WhenCurrentValueLower_ReturnsNegativeFraction()
    {
        var result = ProfitCalculator.CalculateResultFraction(10m, 10m, 80m);

        result.Should().Be(-0.2m);
    }

    [Fact]
    public void CalculateProfitPercent_WhenCostBasisZero_ReturnsNull()
    {
        ProfitCalculator.CalculateProfitPercent(100m, 0m).Should().BeNull();
    }

    [Fact]
    public void CalculateProfitPercent_WhenCurrentValueAboveCostBasis_ReturnsPositivePercent()
    {
        var result = ProfitCalculator.CalculateProfitPercent(120m, 100m);

        result.Should().Be(20m);
    }

    [Fact]
    public void CalculateProfitPercent_WhenCurrentValueBelowCostBasis_ReturnsNegativePercent()
    {
        var result = ProfitCalculator.CalculateProfitPercent(80m, 100m);

        result.Should().Be(-20m);
    }
}
