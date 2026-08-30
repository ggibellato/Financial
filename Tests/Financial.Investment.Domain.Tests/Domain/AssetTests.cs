using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        using (new AssertionScope())
        {
            asset.Name.Should().Be("Asset A");
            asset.ISIN.Should().Be("ISIN123");
            asset.Exchange.Should().Be("NYSE");
            asset.Ticker.Should().Be("AAA");
            asset.Country.Should().Be(CountryCode.Unknown);
            asset.LocalTypeCode.Should().BeEmpty();
            asset.Class.Should().Be(GlobalAssetClass.Unknown);
        }
    }

    [Fact]
    public void UpdateIdentity_SetsAllFieldsAndNormalizesLocalTypeCode()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.UpdateIdentity("Asset B", "ISIN456", "LSE", "BBB", CountryCode.UK, "  Stock  ", GlobalAssetClass.Equity);

        using (new AssertionScope())
        {
            asset.Name.Should().Be("Asset B");
            asset.ISIN.Should().Be("ISIN456");
            asset.Exchange.Should().Be("LSE");
            asset.Ticker.Should().Be("BBB");
            asset.Country.Should().Be(CountryCode.UK);
            asset.LocalTypeCode.Should().Be("Stock");
            asset.Class.Should().Be(GlobalAssetClass.Equity);
        }
    }

    [Fact]
    public void UpdateIdentity_DoesNotDisturbTransactionsOrQuantity()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10, 5m, 0m));

        asset.UpdateIdentity("Asset B", "ISIN456", "LSE", "BBB", CountryCode.UK, "Stock", GlobalAssetClass.Equity);

        asset.Quantity.Should().Be(10);
    }

    [Fact]
    public void Create_CryptocurrencyAssetShape_SetsPropertiesWithBlankIsinAndExchange()
    {
        var asset = Asset.Create("Bitcoin", "", "", "BTC", CountryCode.UK, "", GlobalAssetClass.Cryptocurrency);

        using (new AssertionScope())
        {
            asset.ISIN.Should().BeEmpty();
            asset.Exchange.Should().BeEmpty();
            asset.Ticker.Should().Be("BTC");
            asset.Country.Should().Be(CountryCode.UK);
            asset.Class.Should().Be(GlobalAssetClass.Cryptocurrency);
        }
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
    public void RestorePrice_WhenNothingWasDisplaced_RemovesTheEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);

        asset.RestorePrice(date, previous: null);

        asset.GetPriceForDate(date).Should().BeNull();
        asset.PriceHistory.Should().BeEmpty();
    }

    [Fact]
    public void RestorePrice_WhenAnEntryWasDisplaced_PutsTheOriginalBack()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);
        var displaced = asset.GetPriceForDate(date);
        asset.SetPrice(date, 175m, isManual: false);

        asset.RestorePrice(date, displaced);

        asset.PriceHistory.Should().ContainSingle();
        asset.GetPriceForDate(date)!.Price.Should().Be(100m);
    }

    /// <summary>
    /// RemovePrice refuses an automatic entry so the delete path cannot discard a hand-entered
    /// price. RestorePrice must not inherit that rule: the entry it undoes is the automatic one the
    /// same failed save just wrote, and leaving it behind is what made a lost write read back as a
    /// recorded one.
    /// </summary>
    [Fact]
    public void RestorePrice_OnAnAutomaticEntry_SucceedsWhereRemovePriceRefuses()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);

        asset.RemovePrice(date).Should().BeFalse();
        asset.GetPriceForDate(date).Should().NotBeNull();

        asset.RestorePrice(date, previous: null);

        asset.GetPriceForDate(date).Should().BeNull();
    }

    [Fact]
    public void RestorePrice_ForADateWithNoEntry_LeavesHistoryUntouched()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 90m, isManual: true);

        asset.RestorePrice(new DateOnly(2026, 8, 15), previous: null);

        asset.PriceHistory.Should().ContainSingle();
        asset.GetPriceForDate(new DateOnly(2026, 8, 14))!.Price.Should().Be(90m);
    }

    [Fact]
    public void SetPrice_NewDate_AddsEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);

        asset.SetPrice(date, 100m, isManual: true);

        asset.PriceHistory.Should().ContainSingle();
        asset.GetPriceForDate(date).Should().NotBeNull();
        asset.GetPriceForDate(date)!.Price.Should().Be(100m);
    }

    [Fact]
    public void SetPrice_ExistingDate_ReplacesEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);

        asset.SetPrice(date, 150m, isManual: true);

        asset.PriceHistory.Should().ContainSingle();
        var entry = asset.GetPriceForDate(date);
        entry!.Price.Should().Be(150m);
        entry.IsManual.Should().BeTrue();
    }

    [Fact]
    public void SetPrice_AutomaticThenManualSameDate_ManualEntryWins()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);

        asset.SetPrice(date, 105m, isManual: true);

        var entry = asset.GetPriceForDate(date);
        entry!.IsManual.Should().BeTrue();
        entry.Price.Should().Be(105m);
    }

    [Fact]
    public void SetPrice_DifferentDates_KeepsBothEntries()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: false);

        asset.PriceHistory.Should().HaveCount(2);
    }

    /// <summary>
    /// A price fetch records into the same list the asset page is reading. Editing it in place broke
    /// the reader's enumeration, which is how a save surfaced "Collection was modified". No threads
    /// needed to prove it: mutating part way through a foreach is the same violation.
    /// </summary>
    [Fact]
    public void SetPrice_WhilePriceHistoryIsBeingEnumerated_DoesNotDisturbTheEnumeration()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: false);

        var seen = new List<AssetPriceSnapshot>();
        foreach (var entry in asset.PriceHistory)
        {
            seen.Add(entry);
            asset.SetPrice(new DateOnly(2026, 8, 16), 110m, isManual: false);
        }

        seen.Should().HaveCount(2);
        asset.PriceHistory.Should().HaveCount(3);
    }

    [Fact]
    public void RemovePrice_WhilePriceHistoryIsBeingEnumerated_DoesNotDisturbTheEnumeration()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: true);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: true);

        var seen = new List<AssetPriceSnapshot>();
        foreach (var entry in asset.PriceHistory)
        {
            seen.Add(entry);
            asset.RemovePrice(new DateOnly(2026, 8, 14));
        }

        seen.Should().HaveCount(2);
        asset.PriceHistory.Should().ContainSingle();
    }

    /// <summary>
    /// The guarantee the readers rely on: what PriceHistory handed out stays as it was, so a caller
    /// part way through projecting it never sees a half-applied write.
    /// </summary>
    [Fact]
    public void PriceHistory_TakenBeforeAWrite_IsNotChangedByIt()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        var takenEarlier = asset.PriceHistory;

        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: false);

        takenEarlier.Should().ContainSingle();
        asset.PriceHistory.Should().HaveCount(2);
    }

    [Fact]
    public void GetPriceForDate_NoEntry_ReturnsNull()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);

        var result = asset.GetPriceForDate(new DateOnly(2026, 8, 15));

        result.Should().BeNull();
    }

    [Fact]
    public void RemovePrice_ManualEntry_RemovesAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: true);

        var result = asset.RemovePrice(date);

        result.Should().BeTrue();
        asset.GetPriceForDate(date).Should().BeNull();
    }

    [Fact]
    public void RemovePrice_AutomaticEntry_ReturnsFalseAndKeepsEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);
        asset.SetPrice(date, 100m, isManual: false);

        var result = asset.RemovePrice(date);

        result.Should().BeFalse();
        asset.GetPriceForDate(date).Should().NotBeNull();
    }

    [Fact]
    public void RemovePrice_NoEntryForDate_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.RemovePrice(new DateOnly(2026, 8, 15));

        result.Should().BeFalse();
    }
}
