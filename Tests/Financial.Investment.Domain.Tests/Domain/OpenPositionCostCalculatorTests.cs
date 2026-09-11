using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class OpenPositionCostCalculatorTests
{
    [Fact]
    public void CostOfUnitsHeld_PositiveQuantityAndAveragePrice_ReturnsTheProduct()
    {
        var asset = Asset.Create("Asset A", "ISIN", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        OpenPositionCostCalculator.CostOfUnitsHeld(asset).Should().Be(50m);
    }

    [Fact]
    public void CostOfUnitsHeld_NegativeQuantity_ClampsToZero()
    {
        var asset = Asset.Create("Asset A", "ISIN", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 5m, 10m, 0m));

        asset.Quantity.Should().Be(-5m);
        OpenPositionCostCalculator.CostOfUnitsHeld(asset).Should().Be(0m);
    }

    [Fact]
    public void CostOfUnitsHeld_ZeroQuantity_ReturnsZero()
    {
        var asset = Asset.Create("Asset A", "ISIN", "NYSE", "AAA");

        OpenPositionCostCalculator.CostOfUnitsHeld(asset).Should().Be(0m);
    }
}
