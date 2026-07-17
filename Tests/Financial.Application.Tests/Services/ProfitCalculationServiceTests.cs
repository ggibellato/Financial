using Financial.Application.Services;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class ProfitCalculationServiceTests
{
    private readonly ProfitCalculationService _sut = new();

    [Fact]
    public void HasCostBasis_WhenAveragePriceAndQuantityPositive_ReturnsTrue()
    {
        _sut.HasCostBasis(10m, 5m).Should().BeTrue();
    }

    [Fact]
    public void HasCostBasis_WhenQuantityZero_ReturnsFalse()
    {
        _sut.HasCostBasis(10m, 0m).Should().BeFalse();
    }

    [Fact]
    public void CalculateResultFraction_WhenCurrentValueDoubled_ReturnsOne()
    {
        _sut.CalculateResultFraction(10m, 5m, 100m).Should().Be(1m);
    }

    [Fact]
    public void CalculateResultFraction_WhenNoCostBasis_ReturnsZero()
    {
        _sut.CalculateResultFraction(0m, 5m, 100m).Should().Be(0m);
    }

    [Fact]
    public void CalculateProfitPercent_WhenCostBasisZero_ReturnsNull()
    {
        _sut.CalculateProfitPercent(100m, 0m).Should().BeNull();
    }

    [Fact]
    public void CalculateProfitPercent_WhenCurrentValueAboveCostBasis_ReturnsPositivePercent()
    {
        _sut.CalculateProfitPercent(120m, 100m).Should().Be(20m);
    }
}
