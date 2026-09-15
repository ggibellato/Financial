using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.Helpers;

public class LotAllocationCalculatorTests
{
    private static LotAllocationRowViewModel Row(decimal remaining, decimal quantity = 0m) =>
        new(new OpenLotDTO { SourceTransactionId = Guid.NewGuid(), Date = DateTime.Today, RemainingQuantity = remaining, UnitCost = 10m })
        {
            Quantity = quantity
        };

    [Fact]
    public void SumAllocations_AddsEachRowsQuantity()
    {
        var rows = new[] { Row(10m, 3m), Row(10m, 4m) };

        LotAllocationCalculator.SumAllocations(rows).Should().Be(7m);
    }

    [Fact]
    public void IsAllocationExact_WithinTolerance_ReturnsTrue()
    {
        LotAllocationCalculator.IsAllocationExact(10.0000000001m, 10m).Should().BeTrue();
    }

    [Fact]
    public void IsAllocationExact_OutsideTolerance_ReturnsFalse()
    {
        LotAllocationCalculator.IsAllocationExact(9.9m, 10m).Should().BeFalse();
    }

    [Fact]
    public void IsAllocationExact_ZeroSaleQuantity_ReturnsFalse()
    {
        LotAllocationCalculator.IsAllocationExact(0m, 0m).Should().BeFalse();
    }

    [Fact]
    public void IsLotOverAllocated_QuantityExceedsRemaining_ReturnsTrue()
    {
        var row = Row(remaining: 5m, quantity: 5.1m);

        LotAllocationCalculator.IsLotOverAllocated(row).Should().BeTrue();
    }

    [Fact]
    public void IsLotOverAllocated_QuantityWithinRemaining_ReturnsFalse()
    {
        var row = Row(remaining: 5m, quantity: 5m);

        LotAllocationCalculator.IsLotOverAllocated(row).Should().BeFalse();
    }

    [Fact]
    public void HasOverAllocatedLot_AnyRowOverAllocated_ReturnsTrue()
    {
        var rows = new[] { Row(5m, 5m), Row(5m, 6m) };

        LotAllocationCalculator.HasOverAllocatedLot(rows).Should().BeTrue();
    }

    [Fact]
    public void HasOverAllocatedLot_NoRowOverAllocated_ReturnsFalse()
    {
        var rows = new[] { Row(5m, 5m), Row(5m, 5m) };

        LotAllocationCalculator.HasOverAllocatedLot(rows).Should().BeFalse();
    }
}
