using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

public class XirrCalculationServiceTests
{
    private readonly XirrCalculationService _sut = new();

    [Fact]
    public void Calculate_SingleInvestmentGrowingTenPercent_ReturnsApproximatelyTenPercent()
    {
        var oneYearAgo = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = oneYearAgo, Amount = -1000m }
        };

        var result = _sut.Calculate(cashFlows, 1100m);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.10m, 0.01m);
    }

    [Fact]
    public void Calculate_NoCashFlows_ReturnsNull()
    {
        var result = _sut.Calculate([], 1000m);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_AppendsTerminalValueAsTodaysCashFlow()
    {
        var oneYearAgo = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = oneYearAgo, Amount = -1000m }
        };

        var result = _sut.Calculate(cashFlows, 900m);

        result.Should().NotBeNull();
        result!.Value.Should().BeLessThan(0m);
    }

    [Fact]
    public void Calculate_WithAsOf_AppendsTerminalValueAsOfTheGivenDateNotToday()
    {
        var start = new DateTime(2024, 1, 1);
        var asOf = new DateTime(2025, 1, 1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = start, Amount = -1000m } };

        var result = _sut.Calculate(cashFlows, 1100m, asOf);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.10m, 0.01m);
    }

    [Fact]
    public void Calculate_TwoArgOverload_MatchesTheAsOfOverloadCalledWithToday()
    {
        var oneYearAgo = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = oneYearAgo, Amount = -1000m } };

        var twoArgResult = _sut.Calculate(cashFlows, 1100m);
        var asOfResult = _sut.Calculate(cashFlows, 1100m, DateTime.Today);

        asOfResult.Should().Be(twoArgResult);
    }
}
