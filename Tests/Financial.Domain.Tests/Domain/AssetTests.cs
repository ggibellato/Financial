using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.Name.Should().Be("Asset A");
        asset.ISIN.Should().Be("ISIN123");
        asset.Exchange.Should().Be("NYSE");
        asset.Ticker.Should().Be("AAA");
        asset.Country.Should().Be(CountryCode.Unknown);
        asset.LocalTypeCode.Should().BeEmpty();
        asset.Class.Should().Be(GlobalAssetClass.Unknown);
    }

    [Fact]
    public void AddTransaction_Buy_UpdatesAveragePriceAndQuantity()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var first = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var second = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);

        asset.AddTransaction(first);
        asset.AddTransaction(second);

        asset.Quantity.Should().Be(20m);
        asset.AveragePrice.Should().Be(6m);
        asset.Active.Should().BeTrue();
    }

    [Fact]
    public void AddTransaction_Sell_DecreasesQuantityAndKeepsAveragePrice()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));

        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        asset.Quantity.Should().Be(0m);
        asset.AveragePrice.Should().Be(10m);
        asset.Active.Should().BeFalse();
    }

    [Fact]
    public void UpdateTransaction_RebuildsTransactionsAndRecalculates()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var tx1Id = Guid.NewGuid();
        var tx1 = Transaction.CreateWithId(tx1Id, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var tx2 = Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);
        asset.AddTransaction(tx1);
        asset.AddTransaction(tx2);

        var updated = Transaction.CreateWithId(tx1Id, tx1.Date, tx1.Type, 20m, 5m, 0m);
        var result = asset.UpdateTransaction(updated);

        result.Should().BeTrue();
        asset.Quantity.Should().Be(30m);
        var expected = (20m * 5m + 10m * 7m) / 30m;
        asset.AveragePrice.Should().Be(expected);
    }

    [Fact]
    public void UpdateTransaction_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.UpdateTransaction(Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateTransaction_EmptyId_Throws()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        // Activator bypasses the public factory methods so Id stays Guid.Empty.
        var transaction = (Transaction)Activator.CreateInstance(typeof(Transaction), nonPublic: true)!;

        Action act = () => asset.UpdateTransaction(transaction);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveTransaction_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.RemoveTransaction(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RemoveTransaction_EmptyId_Throws()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.RemoveTransaction(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddCredit_AddsToCollection()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m);

        asset.AddCredit(credit);

        asset.Credits.Should().ContainSingle()
            .Which.Should().Be(credit);
    }

    [Fact]
    public void UpdateCredit_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.UpdateCredit(Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m));

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateCredit_EmptyId_Throws()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m);

        Action act = () => asset.UpdateCredit(credit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveCredit_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.RemoveCredit(Guid.NewGuid()).Should().BeFalse();
    }
}
