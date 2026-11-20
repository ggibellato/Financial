using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class XirrCalculationServiceTests
{
    private readonly XirrCalculationService _sut = new(TestClock.At());

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
    public void Calculate_WithoutAnAsOfDate_MeasuresUpToToday()
    {
        var oneYearAgo = TestClock.LocalToday.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = oneYearAgo, Amount = -1000m } };

        var result = _sut.Calculate(cashFlows, 1100m);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.10m, 0.01m);
    }
}
