using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class AssetTotalsCalculatorTests
{
    [Fact]
    public void CalculateTotals_WithNoTransactionsOrCredits_ReturnsAllZero()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var (totalBought, totalSold, totalCredits) = AssetTotalsCalculator.CalculateTotals(asset);

        totalBought.Should().Be(0m);
        totalSold.Should().Be(0m);
        totalCredits.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotals_SumsBuyTransactionsIntoTotalBought()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 4m, 10m, 0m));

        var (totalBought, totalSold, _) = AssetTotalsCalculator.CalculateTotals(asset);

        totalBought.Should().Be(90m);
        totalSold.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotals_SumsSellTransactionsIntoTotalSold()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 3m, 8m, 0m));

        var (totalBought, totalSold, _) = AssetTotalsCalculator.CalculateTotals(asset);

        totalBought.Should().Be(50m);
        totalSold.Should().Be(24m);
    }

    [Fact]
    public void CalculateTotals_SumsCreditsRegardlessOfType()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 12.5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Rent, 7.5m));

        var (_, _, totalCredits) = AssetTotalsCalculator.CalculateTotals(asset);

        totalCredits.Should().Be(20m);
    }

    [Fact]
    public void CalculateTotals_UsesEachTransactionsTotalPrice_NotQuantityTimesUnitPrice()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 1.5m));

        var (totalBought, _, _) = AssetTotalsCalculator.CalculateTotals(asset);

        totalBought.Should().Be(51.5m);
    }
}
