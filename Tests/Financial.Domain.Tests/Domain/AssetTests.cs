using Financial.Domain.Entities;
using FluentAssertions;
using System.Linq;

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
    public void Create_CryptocurrencyAssetShape_SetsPropertiesWithBlankIsinAndExchange()
    {
        var asset = Asset.Create("Bitcoin", "", "", "BTC", CountryCode.UK, "", GlobalAssetClass.Cryptocurrency);

        asset.ISIN.Should().BeEmpty();
        asset.Exchange.Should().BeEmpty();
        asset.Ticker.Should().Be("BTC");
        asset.Country.Should().Be(CountryCode.UK);
        asset.Class.Should().Be(GlobalAssetClass.Cryptocurrency);
    }

    [Fact]
    public void Create_BlankTicker_StillCreatesAssetWithEmptyTicker()
    {
        var asset = Asset.Create("Bitcoin", "", "", "", CountryCode.UK, "", GlobalAssetClass.Cryptocurrency);

        asset.Ticker.Should().BeEmpty();
    }

    [Fact]
    public void Create_FiveArgOverload_ResolvesAssetClassFromCountryAndLocalTypeCode()
    {
        var asset = Asset.Create("Petrobras", "ISIN123", "BVMF", "PETR4", CountryCode.BR, "Acoes");

        asset.Country.Should().Be(CountryCode.BR);
        asset.LocalTypeCode.Should().Be("Acoes");
        asset.Class.Should().Be(GlobalAssetClass.Equity);
    }

    [Fact]
    public void AddTransaction_NullTransaction_ThrowsArgumentNullException()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.AddTransaction(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PositionType_PositiveQuantity_ReturnsLong()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        asset.PositionType.Should().Be(PositionType.Long);
    }

    [Fact]
    public void PositionType_ZeroQuantity_ReturnsFlat()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.PositionType.Should().Be(PositionType.Flat);
    }

    [Fact]
    public void PositionType_NegativeQuantity_ReturnsShort()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 5m, 10m, 0m));

        asset.Quantity.Should().Be(-5m);
        asset.PositionType.Should().Be(PositionType.Short);
    }

    [Fact]
    public void UpdateTransaction_NullTransaction_ThrowsArgumentNullException()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.UpdateTransaction(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveTransaction_ExistingId_RemovesAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var txId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var result = asset.RemoveTransaction(txId);

        result.Should().BeTrue();
        asset.Transactions.Should().BeEmpty();
        asset.Quantity.Should().Be(0m);
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
    public void AddCredit_NullCredit_ThrowsArgumentNullException()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.AddCredit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCredits_AddsAllCreditsToCollection()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var credits = new[]
        {
            Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m),
            Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 3, 1), Credit.CreditType.Rent, 20m),
        };

        asset.AddCredits(credits);

        asset.Credits.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateCredit_NullCredit_ThrowsArgumentNullException()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.UpdateCredit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateCredit_ExistingId_UpdatesAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m));
        var updated = Credit.CreateWithId(creditId, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 25m);

        var result = asset.UpdateCredit(updated);

        result.Should().BeTrue();
        asset.Credits.Should().ContainSingle().Which.Value.Should().Be(25m);
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

    [Fact]
    public void RemoveCredit_ExistingId_RemovesAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m));

        var result = asset.RemoveCredit(creditId);

        result.Should().BeTrue();
        asset.Credits.Should().BeEmpty();
    }

    [Fact]
    public void Quantity_AveragePrice_RealizedGainLoss_ReflectTransactionsAndCredits()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));

        asset.Quantity.Should().Be(asset.Transactions.Quantity);
        asset.AveragePrice.Should().Be(asset.Transactions.AveragePrice);
        asset.AverageSellPrice.Should().Be(asset.Transactions.AverageSellPrice);
        asset.RealizedGainLoss.Should().Be(asset.Transactions.RealizedCapitalGain + asset.Credits.Sum(c => c.Value));
    }
}
