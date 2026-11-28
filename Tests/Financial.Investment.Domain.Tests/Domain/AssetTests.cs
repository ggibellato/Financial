using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
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
        asset.AddTransaction(Transaction.Create(TestClock.Today.ToDateTime(TimeOnly.MinValue), Transaction.TransactionType.Buy, 10, 5m, 0m));

        asset.UpdateIdentity("Asset B", "ISIN456", "LSE", "BBB", CountryCode.UK, "Stock", GlobalAssetClass.Equity);

        asset.Quantity.Should().Be(10);
    }

    [Theory]
    [InlineData(GlobalAssetClass.Equity)]
    [InlineData(GlobalAssetClass.RealEstate)]
    public void SetSegment_ClassWithSegments_StoresTheTrimmedSegment(GlobalAssetClass assetClass)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", assetClass);

        asset.SetSegment("  Logistics  ");

        asset.Segment.Should().Be("Logistics");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSegment_BlankSegment_StoresNoSegment(string? segment)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.RealEstate);
        asset.SetSegment("Logistics");

        asset.SetSegment(segment);

        asset.Segment.Should().BeNull();
    }

    [Fact]
    public void SetSegment_ClassWithoutSegments_StoresNoSegment()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "LSE", "AAA", CountryCode.UK, "", GlobalAssetClass.ETF);

        asset.SetSegment("Logistics");

        asset.Segment.Should().BeNull();
    }

    [Theory]
    [InlineData(CountryCode.BR, GlobalAssetClass.Equity, "11222333000181")]
    [InlineData(CountryCode.BR, GlobalAssetClass.RealEstate, "11222333000181")]
    [InlineData(CountryCode.BR, GlobalAssetClass.Fund, "11222333000181")]
    [InlineData(CountryCode.BR, GlobalAssetClass.Bond, "11222333000181")]
    [InlineData(CountryCode.BR, GlobalAssetClass.Pension, "11222333000181")]
    [InlineData(CountryCode.UK, GlobalAssetClass.Equity, null)]
    [InlineData(CountryCode.US, GlobalAssetClass.Fund, null)]
    [InlineData(CountryCode.Unknown, GlobalAssetClass.Unknown, null)]
    public void SetCnpj_AppliesToEveryBrazilianAssetOnly(CountryCode country, GlobalAssetClass assetClass, string? expected)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", country, "", assetClass);

        asset.SetCnpj("11.222.333/0001-81");

        asset.Cnpj.Should().Be(expected);
    }

    [Theory]
    [InlineData(CountryCode.BR, GlobalAssetClass.Fund, "11444777000161")]
    [InlineData(CountryCode.BR, GlobalAssetClass.RealEstate, "11444777000161")]
    [InlineData(CountryCode.BR, GlobalAssetClass.ETF, "11444777000161")]
    [InlineData(CountryCode.BR, GlobalAssetClass.Equity, null)]
    [InlineData(CountryCode.BR, GlobalAssetClass.Pension, null)]
    [InlineData(CountryCode.UK, GlobalAssetClass.RealEstate, null)]
    public void SetAdministratorCnpj_AppliesOnlyToAcceptedBrazilianClasses(CountryCode country, GlobalAssetClass assetClass, string? expected)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", country, "", assetClass);

        asset.SetAdministratorCnpj("11444777000161");

        asset.AdministratorCnpj.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetCnpjField_Blank_ClearsIt(bool administrator)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Fund);
        asset.SetCnpj("11222333000181");
        asset.SetAdministratorCnpj("11444777000161");

        if (administrator)
        {
            asset.SetAdministratorCnpj("  ");
            asset.AdministratorCnpj.Should().BeNull();
        }
        else
        {
            asset.SetCnpj("  ");
            asset.Cnpj.Should().BeNull();
        }
    }

    [Fact]
    public void SetCnpj_InvalidValueOnANonBrazilianAsset_IsRefused()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "LSE", "AAA", CountryCode.UK, "", GlobalAssetClass.Equity);

        var act = () => asset.SetCnpj("11222333000182");

        act.Should().Throw<ArgumentException>().WithMessage("Enter a valid 14-digit CNPJ.");
    }

    [Theory]
    [InlineData(CountryCode.UK, GlobalAssetClass.Fund, null, null)]
    [InlineData(CountryCode.BR, GlobalAssetClass.Equity, "11222333000181", null)]
    [InlineData(CountryCode.BR, GlobalAssetClass.Fund, "11222333000181", "11444777000161")]
    public void UpdateIdentity_ClearsInapplicableCnpjs(
        CountryCode country, GlobalAssetClass assetClass, string? expectedCnpj, string? expectedAdministratorCnpj)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Fund);
        asset.SetCnpj("11222333000181");
        asset.SetAdministratorCnpj("11444777000161");

        asset.UpdateIdentity("Asset B", "ISIN123", "BVMF", "AAA", country, "", assetClass);

        using (new AssertionScope())
        {
            asset.Cnpj.Should().Be(expectedCnpj);
            asset.AdministratorCnpj.Should().Be(expectedAdministratorCnpj);
        }
    }

    [Fact]
    public void UpdateIdentity_EtfToEquity_ClearsOnlyTheAdministratorCnpj()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.ETF);
        asset.SetCnpj("11222333000181");
        asset.SetAdministratorCnpj("11444777000161");

        asset.UpdateIdentity("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Equity);

        using (new AssertionScope())
        {
            asset.Cnpj.Should().Be("11222333000181");
            asset.AdministratorCnpj.Should().BeNull();
        }
    }

    [Fact]
    public void SetSegment_FortyCharacters_IsAccepted()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Equity);

        asset.SetSegment(new string('a', 40));

        asset.Segment.Should().HaveLength(40);
    }

    [Fact]
    public void SetSegment_LongerThanFortyCharacters_IsRefused()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Equity);

        var act = () => asset.SetSegment(new string('a', 41));

        act.Should().Throw<ArgumentException>().WithMessage("Segment must be 40 characters or fewer.");
    }

    [Fact]
    public void UpdateIdentity_ClassChangedToOneWithoutSegments_RemovesTheSegment()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.RealEstate);
        asset.SetSegment("Logistics");

        asset.UpdateIdentity("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.ETF);

        asset.Segment.Should().BeNull();
    }

    [Fact]
    public void UpdateIdentity_ClassChangedBetweenClassesWithSegments_KeepsTheSegment()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.RealEstate);
        asset.SetSegment("Logistics");

        asset.UpdateIdentity("Asset A", "ISIN123", "BVMF", "AAA", CountryCode.BR, "", GlobalAssetClass.Equity);

        asset.Segment.Should().Be("Logistics");
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
    public void RecordTransaction_SaleExceedsHeldQuantity_ThrowsAndAddsNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        Action act = () => asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 15m, 6m, 0m));

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*10*");
        asset.Transactions.Should().ContainSingle();
        asset.Quantity.Should().Be(10m);
    }

    [Fact]
    public void RecordTransaction_SaleOfExactlyHeldQuantity_Succeeds()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 6m, 0m));

        asset.Quantity.Should().Be(0m);
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Redemption)]
    [InlineData(Transaction.TransactionType.TransferOut)]
    public void RecordTransaction_TypeExceedsHeldQuantity_ThrowsAndAddsNothing(Transaction.TransactionType type)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        Action act = () => asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 2, 1), type, 15m, 6m, 0m));

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*10*");
        asset.Transactions.Should().ContainSingle();
        asset.Quantity.Should().Be(10m);
    }

    [Fact]
    public void ReviseTransaction_EditLeavesTheEditedSaleUncovered_ThrowsAndChangesNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var saleId = Guid.NewGuid();
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.CreateWithId(saleId, new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 6m, 0m));

        var oversized = Transaction.CreateWithId(saleId, new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 20m, 6m, 0m);
        Action act = () => asset.ReviseTransaction(oversized);

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.Quantity.Should().Be(5m);
    }

    [Fact]
    public void ReviseTransaction_EditLeavesALaterSaleUncovered_ThrowsNamingTheLaterSale()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var editedPurchaseId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(editedPurchaseId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 100m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Sell, 80m, 6m, 0m));

        var reducedPurchase = Transaction.CreateWithId(editedPurchaseId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 50m, 5m, 0m);
        Action act = () => asset.ReviseTransaction(reducedPurchase);

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*30*");
        asset.Transactions.Should().Contain(t => t.Id == editedPurchaseId && t.Quantity == 100m);
    }

    [Fact]
    public void ReviseTransaction_UnknownId_ReturnsFalseAndChangesNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var result = asset.ReviseTransaction(Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
        asset.Quantity.Should().Be(10m);
    }

    [Fact]
    public void RetractTransaction_DeletingThePurchaseThatFundedALaterSale_ThrowsAndDeletesNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var purchaseId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(purchaseId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 6m, 0m));

        Action act = () => asset.RetractTransaction(purchaseId);

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public void RetractTransaction_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.RetractTransaction(Guid.NewGuid());

        result.Should().BeFalse();
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
            Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 3, 1), Credit.CreditType.SecuritiesLendingIncome, 20m),
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
    public void SetPrice_NewDate_AddsEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var date = new DateOnly(2026, 8, 15);

        asset.SetPrice(date, 100m, isManual: true);

        asset.PriceSnapshots.Should().ContainSingle();
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

        asset.PriceSnapshots.Should().ContainSingle();
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

        asset.PriceSnapshots.Should().HaveCount(2);
    }

    [Fact]
    public void GetMostRecentPrice_NoHistory_ReturnsNull()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.GetMostRecentPrice().Should().BeNull();
    }

    [Fact]
    public void GetMostRecentPrice_SeveralEntries_ReturnsTheLatestByDate()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 10), 90m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 15), 100m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 12), 95m, isManual: false);

        var entry = asset.GetMostRecentPrice();

        entry!.Date.Should().Be(new DateOnly(2026, 8, 15));
        entry.Price.Should().Be(100m);
    }

    /// <summary>
    /// A price fetch records into the same list the asset page is reading. Editing it in place broke
    /// the reader's enumeration, which is how a save surfaced "Collection was modified". No threads
    /// needed to prove it: mutating part way through a foreach is the same violation.
    /// </summary>
    [Fact]
    public void SetPrice_WhilePriceSnapshotsIsBeingEnumerated_DoesNotDisturbTheEnumeration()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: false);

        var seen = new List<AssetPriceSnapshot>();
        foreach (var entry in asset.PriceSnapshots)
        {
            seen.Add(entry);
            asset.SetPrice(new DateOnly(2026, 8, 16), 110m, isManual: false);
        }

        seen.Should().HaveCount(2);
        asset.PriceSnapshots.Should().HaveCount(3);
    }

    [Fact]
    public void RemovePrice_WhilePriceSnapshotsIsBeingEnumerated_DoesNotDisturbTheEnumeration()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: true);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: true);

        var seen = new List<AssetPriceSnapshot>();
        foreach (var entry in asset.PriceSnapshots)
        {
            seen.Add(entry);
            asset.RemovePrice(new DateOnly(2026, 8, 14));
        }

        seen.Should().HaveCount(2);
        asset.PriceSnapshots.Should().ContainSingle();
    }

    /// <summary>
    /// The guarantee the readers rely on: what PriceSnapshots handed out stays as it was, so a caller
    /// part way through projecting it never sees a half-applied write.
    /// </summary>
    [Fact]
    public void PriceSnapshots_TakenBeforeAWrite_IsNotChangedByIt()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        var takenEarlier = asset.PriceSnapshots;

        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: false);

        takenEarlier.Should().ContainSingle();
        asset.PriceSnapshots.Should().HaveCount(2);
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

    [Fact]
    public void GetPriceAsOf_ExactDateMatch_ReturnsThatEntry()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);

        asset.GetPriceAsOf(new DateOnly(2026, 8, 14))!.Price.Should().Be(100m);
    }

    [Fact]
    public void GetPriceAsOf_NoExactMatch_ReturnsTheMostRecentEntryOnOrBefore()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 10), 90m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);

        asset.GetPriceAsOf(new DateOnly(2026, 8, 17))!.Price.Should().Be(100m);
    }

    [Fact]
    public void GetPriceAsOf_EntryAfterTheAsOfDate_IsIgnored()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 10), 90m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 20), 999m, isManual: false);

        asset.GetPriceAsOf(new DateOnly(2026, 8, 14))!.Price.Should().Be(90m);
    }

    [Fact]
    public void GetPriceAsOf_NoEntryOnOrBeforeTheAsOfDate_ReturnsNull()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.SetPrice(new DateOnly(2026, 8, 20), 100m, isManual: false);

        asset.GetPriceAsOf(new DateOnly(2026, 8, 14)).Should().BeNull();
    }

    [Fact]
    public void GetPriceAsOf_NoPriceSnapshots_ReturnsNull()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.GetPriceAsOf(new DateOnly(2026, 8, 14)).Should().BeNull();
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Sell)]
    [InlineData(Transaction.TransactionType.Redemption)]
    [Trait("AC", "P50-F02-disposal-record-01")]
    public void RecordTransaction_SellingType_CreatesExactlyOneActiveRealisedGain(Transaction.TransactionType type)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), type, 4m, 6m, 0m));

        asset.RealisedGains.Should().ContainSingle();
        asset.RealisedGains.Single().Status.Should().Be(RealisedGainStatus.Active);
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Buy)]
    [InlineData(Transaction.TransactionType.TransferIn)]
    [InlineData(Transaction.TransactionType.TransferOut)]
    [Trait("AC", "P50-F02-disposal-record-02")]
    public void RecordTransaction_NonSellingType_CreatesNoRealisedGain(Transaction.TransactionType type)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), type, 4m, 6m, 0m));

        asset.RealisedGains.Should().BeEmpty();
    }

    [Fact]
    public void RecordTransaction_Fee_CreatesNoRealisedGain()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Fee, 0m, 0m, 3m));

        asset.RealisedGains.Should().BeEmpty();
    }

    [Fact]
    public void RealisedGainLoss_UnderAverageCost_MatchesPreFeatureWeightedAverageFigure()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));

        asset.RealisedGainLoss.Should().Be(62m, "550 proceeds minus 5x100 cost basis, plus 12 in credits");
    }

    [Fact]
    public void RealisedGainLossSharesOnly_ExcludesCredits_MatchesRealisedGainSumAlone()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));

        asset.RealisedGainLossSharesOnly.Should().Be(50m, "550 proceeds minus 5x100 cost basis, with the 12 credit excluded");
        asset.RealisedGainLoss.Should().Be(62m);
    }

    [Fact]
    [Trait("AC", "P50-F02-disposal-record-07")]
    public void RecordTransaction_UnderFifo_TransactionsRealisedCapitalGainReflectsRealisedGainsNotWeightedAverage()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 200m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 1, 3), Transaction.TransactionType.Sell, 10m, 250m, 0m), CostBasisMethod.FIFO);

        asset.Transactions.RealisedCapitalGain.Should().Be(1500m, "FIFO consumes the 100-cost lot first (2500 proceeds - 1000 cost), not the 150 weighted-average cost");
    }

    [Fact]
    [Trait("AC", "P50-F02-disposal-record-08")]
    public void RecordTransaction_SpecificIdAllocationMismatch_ThrowsAndAddsNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var lotId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(lotId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var allocation = new[] { new SpecificLotAllocation(lotId, 3m) };
        Action act = () => asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 4m, 6m, 0m),
            CostBasisMethod.SpecificId,
            allocation);

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.Transactions.Should().ContainSingle();
        asset.RealisedGains.Should().BeEmpty();
    }

    [Fact]
    public void RecordTransaction_BackdatedBuyAtOrBeforeLatestSale_RegeneratesExistingSale()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        asset.RecordTransaction(Transaction.Create(new DateTime(2023, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));

        original.Status.Should().Be(RealisedGainStatus.Superseded);
        asset.RealisedGains.Should().HaveCount(2);
    }

    [Fact]
    public void RecordTransaction_ForwardDatedAdd_DoesNotRegenerateExistingSale()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 5m, 90m, 0m));

        original.Status.Should().Be(RealisedGainStatus.Active);
        asset.RealisedGains.Should().ContainSingle();
    }

    [Fact]
    [Trait("AC", "P50-F03-disposal-recalculation-policy-01")]
    public void ReviseTransaction_BackdatedEditAtOrBeforeLatestSale_RegeneratesExistingSale()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy);
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        var revisedBuy = Transaction.CreateWithId(buy.Id, buy.Date, buy.Type, buy.Quantity, 50m, buy.Fees, buy.Withheld, buy.Currency, buy.FxRateSnapshot);
        asset.ReviseTransaction(revisedBuy);

        original.Status.Should().Be(RealisedGainStatus.Superseded);
        asset.RealisedGains.Should().HaveCount(2);
    }

    [Fact]
    [Trait("AC", "P50-F03-disposal-recalculation-policy-01")]
    public void RetractTransaction_DeletingASaleAtOrBeforeLatestSale_RegeneratesAndRetiresItsRecord()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        var firstSell = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m);
        asset.RecordTransaction(firstSell);
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 5m, 160m, 0m));
        var firstRecord = asset.RealisedGains.First(r => r.TransactionId == firstSell.Id);

        asset.RetractTransaction(firstSell.Id);

        firstRecord.Status.Should().Be(RealisedGainStatus.Superseded);
        firstRecord.SupersededByRecordId.Should().BeNull();
        asset.RealisedGains.Where(r => r.Status == RealisedGainStatus.Active).Should().HaveCount(1);
    }

    [Fact]
    [Trait("AC", "P50-F03-disposal-recalculation-policy-06")]
    public void ReviseTransaction_RegenerationThrows_RevertsTheEditAndRethrows()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var buy1 = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        var buy2 = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy1);
        asset.AddTransaction(buy2);
        var sell = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 150m, 0m);
        asset.RecordTransaction(sell, CostBasisMethod.SpecificId, new[] { new SpecificLotAllocation(buy2.Id, 10m) });

        var shrunkBuy2 = Transaction.CreateWithId(buy2.Id, buy2.Date, buy2.Type, 5m, buy2.UnitPrice, buy2.Fees, buy2.Withheld, buy2.Currency, buy2.FxRateSnapshot);
        Action act = () => asset.ReviseTransaction(shrunkBuy2, CostBasisMethod.SpecificId);

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.Transactions.Single(t => t.Id == buy2.Id).Quantity.Should().Be(10m);
        asset.RealisedGains.Single().Status.Should().Be(RealisedGainStatus.Active);
    }

    [Fact]
    public void RecordTransaction_BackdatedSaleTheRegenerationRejects_RemovesTheSaleAndRethrows()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var buy1 = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        var buy2 = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy1);
        asset.AddTransaction(buy2);
        asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m),
            CostBasisMethod.SpecificId,
            new[] { new SpecificLotAllocation(buy2.Id, 5m) });
        var backdatedSale = Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 3m, 150m, 0m);

        Action act = () => asset.RecordTransaction(
            backdatedSale, CostBasisMethod.SpecificId, new[] { new SpecificLotAllocation(buy1.Id, 2m) });

        act.Should().Throw<InvestmentRuleViolationException>();
        using (new AssertionScope())
        {
            asset.Transactions.Should().NotContain(t => t.Id == backdatedSale.Id);
            asset.Quantity.Should().Be(15m);
            asset.RealisedGains.Should().ContainSingle().Which.Status.Should().Be(RealisedGainStatus.Active);
        }
    }

    [Fact]
    public void RetractTransaction_RegenerationRejectsTheRemainingSales_RestoresTheTransactionAndRethrows()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var buy1 = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        var buy2 = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy1);
        asset.AddTransaction(buy2);
        asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m),
            CostBasisMethod.SpecificId,
            new[] { new SpecificLotAllocation(buy1.Id, 5m) });

        Action act = () => asset.RetractTransaction(buy1.Id, CostBasisMethod.SpecificId);

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage($"*{buy1.Id}*");
        using (new AssertionScope())
        {
            asset.Transactions.Should().Contain(t => t.Id == buy1.Id);
            asset.Quantity.Should().Be(15m);
            asset.RealisedGains.Should().ContainSingle().Which.Status.Should().Be(RealisedGainStatus.Active);
        }
    }

    [Fact]
    public void AppendTaxClassification_AddsToTaxClassifications()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var classification = TaxClassification.CreateForCredit(
            Guid.NewGuid(), Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Incomplete, null, TestClock.Default);

        asset.AppendTaxClassification(classification);

        asset.TaxClassifications.Should().ContainSingle().Which.Should().BeSameAs(classification);
    }

    [Fact]
    public void FindTaxClassificationBySource_MatchingSourceTypeAndId_ReturnsIt()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var sourceId = Guid.NewGuid();
        var classification = TaxClassification.CreateForCredit(
            sourceId, Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Incomplete, null, TestClock.Default);
        asset.AppendTaxClassification(classification);

        var found = asset.FindTaxClassificationBySource(SourceType.Credit, sourceId);

        found.Should().BeSameAs(classification);
    }

    [Fact]
    public void FindTaxClassificationBySource_NoMatch_ReturnsNull()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var found = asset.FindTaxClassificationBySource(SourceType.Credit, Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public void SupersedeTaxClassificationBySource_MatchingEntry_SupersedesIt()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var sourceId = Guid.NewGuid();
        var classification = TaxClassification.CreateForCredit(
            sourceId, Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Incomplete, null, TestClock.Default);
        asset.AppendTaxClassification(classification);
        var replacementId = Guid.NewGuid();

        asset.SupersedeTaxClassificationBySource(SourceType.Credit, sourceId, replacementId);

        using (new AssertionScope())
        {
            classification.Status.Should().Be(TaxClassificationStatus.Superseded);
            classification.SupersededByClassificationId.Should().Be(replacementId);
        }
    }

    [Fact]
    public void SupersedeTaxClassificationBySource_NoMatch_DoesNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.SupersedeTaxClassificationBySource(SourceType.Credit, Guid.NewGuid(), null);

        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveTaxClassificationBySource_MatchingEntry_RemovesItAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var sourceId = Guid.NewGuid();
        asset.AppendTaxClassification(TaxClassification.CreateForCredit(
            sourceId, Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Incomplete, null, TestClock.Default));

        var removed = asset.RemoveTaxClassificationBySource(SourceType.Credit, sourceId);

        using (new AssertionScope())
        {
            removed.Should().BeTrue();
            asset.TaxClassifications.Should().BeEmpty();
        }
    }

    [Fact]
    public void RemoveTaxClassificationBySource_NoMatch_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var removed = asset.RemoveTaxClassificationBySource(SourceType.Credit, Guid.NewGuid());

        removed.Should().BeFalse();
    }

    [Fact]
    public void RecordTransaction_WithInvestmentsAndApplicableRule_ClassifiesTheSaleAsFinal()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();
        var rule = investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.CapitalGain, "BR capital gains", "desc", new DateOnly(2021, 1, 1), null);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m), investments: investments);

        var classification = asset.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.RealisedGain);
        classification.CalculationStatus.Should().Be(CalculationStatus.Final);
        classification.TaxRuleId.Should().Be(rule.Id);
    }

    [Fact]
    public void RecordTransaction_WithInvestmentsAndNoApplicableRule_ClassifiesTheSaleAsIncomplete()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m), investments: investments);

        asset.TaxClassifications.Should().ContainSingle().Which.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
    }

    [Fact]
    public void RecordTransaction_WithoutInvestments_CreatesNoClassification()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        asset.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void AddCredit_WithInvestments_ClassifiesTheCredit()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();

        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m), investments);

        var classification = asset.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.Credit);
        classification.EventCategory.Should().Be(EventCategory.Dividend);
    }

    [Fact]
    public void AddCredit_WithoutInvestments_CreatesNoClassification()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");

        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m));

        asset.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCredit_WithInvestments_ReplacesTheExistingClassification()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m), investments);
        var originalClassification = asset.TaxClassifications.Single();

        asset.UpdateCredit(Credit.CreateWithId(creditId, new DateTime(2026, 6, 1), Credit.CreditType.Coupon, 200m), investments);

        using (new AssertionScope())
        {
            asset.TaxClassifications.Should().ContainSingle();
            var updated = asset.TaxClassifications.Single();
            updated.Id.Should().NotBe(originalClassification.Id);
            updated.EventCategory.Should().Be(EventCategory.Interest);
            updated.GrossAmount.Should().Be(200m);
        }
    }

    [Fact]
    public void UpdateCredit_WithoutInvestments_RemovesTheStaleClassificationButCreatesNoNewOne()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m), investments);

        asset.UpdateCredit(Credit.CreateWithId(creditId, new DateTime(2026, 6, 1), Credit.CreditType.Coupon, 200m));

        asset.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCredit_RemovesItsMatchingClassification()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var investments = Investments.Create();
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m), investments);

        asset.RemoveCredit(creditId);

        asset.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void RecordTransaction_BuyOnTheLatestSaleDate_RegeneratesTheSaleAndAddsTheBuyOnce()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));

        using var _ = new AssertionScope();
        original.Status.Should().Be(RealisedGainStatus.Superseded);
        asset.Transactions.Should().HaveCount(3);
    }

    [Fact]
    public void RecordTransaction_BuyBetweenTwoSales_RegeneratesTheLaterOne()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var laterSale = Transaction.Create(new DateTime(2024, 4, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m);
        asset.RecordTransaction(laterSale);
        var laterRecord = asset.RealisedGains.Single(r => r.TransactionId == laterSale.Id);

        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));

        laterRecord.Status.Should().Be(RealisedGainStatus.Superseded);
    }

    [Theory]
    [InlineData(2024, 3, 1, 2024, 1, 15)]
    [InlineData(2024, 1, 15, 2024, 3, 1)]
    public void ReviseTransaction_MovingABuyAcrossTheLatestSale_RegeneratesIt(int fromYear, int fromMonth, int fromDay, int toYear, int toMonth, int toDay)
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var movedBuy = Transaction.Create(new DateTime(fromYear, fromMonth, fromDay), Transaction.TransactionType.Buy, 10m, 50m, 0m);
        asset.AddTransaction(movedBuy);
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        var revised = Transaction.CreateWithId(movedBuy.Id, new DateTime(toYear, toMonth, toDay), movedBuy.Type, movedBuy.Quantity, movedBuy.UnitPrice, movedBuy.Fees);
        var result = asset.ReviseTransaction(revised);

        using var _ = new AssertionScope();
        result.Should().BeTrue();
        original.Status.Should().Be(RealisedGainStatus.Superseded);
    }

    [Fact]
    public void ReviseTransaction_BuyOnTheLatestSaleDate_RegeneratesIt()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var sameDayBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m);
        asset.AddTransaction(sameDayBuy);
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        asset.ReviseTransaction(Transaction.CreateWithId(sameDayBuy.Id, sameDayBuy.Date, sameDayBuy.Type, sameDayBuy.Quantity, 60m, sameDayBuy.Fees));

        original.Status.Should().Be(RealisedGainStatus.Superseded);
    }

    [Fact]
    public void RetractTransaction_BuyOnTheLatestSaleDate_RegeneratesItAndReturnsTrue()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var sameDayBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 50m, 0m);
        asset.AddTransaction(sameDayBuy);
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        var original = asset.RealisedGains.Single();

        var result = asset.RetractTransaction(sameDayBuy.Id);

        using var _ = new AssertionScope();
        result.Should().BeTrue();
        original.Status.Should().Be(RealisedGainStatus.Superseded);
    }

    [Fact]
    public void FindTaxClassificationBySource_MatchesTheSourceIdNotJustItsType()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var first = TaxClassification.CreateForCredit(
            Guid.NewGuid(), Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Final, null, TestClock.Default);
        var second = TaxClassification.CreateForCredit(
            Guid.NewGuid(), Jurisdiction.BR, "2026", EventCategory.Dividend, 50m, 0m, 50m, CalculationStatus.Final, null, TestClock.Default);
        asset.AppendTaxClassification(first);
        asset.AppendTaxClassification(second);

        asset.FindTaxClassificationBySource(SourceType.Credit, second.SourceId).Should().BeSameAs(second);
    }

    [Fact]
    public void RemoveCredit_EmptyId_Throws()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddCredit(Credit.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m));

        var act = () => asset.RemoveCredit(Guid.Empty);

        act.Should().Throw<ArgumentException>();
        asset.Credits.Should().ContainSingle();
    }

    [Fact]
    [Trait("AC", "P50-F02-disposal-record-07")]
    public void RealisedGainLossSharesOnly_SumsEveryActiveRealisedGain()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 150m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 5m, 120m, 0m));

        asset.RealisedGainLossSharesOnly.Should().Be(asset.RealisedGains.Sum(r => r.GainLoss)).And.Be(350m);
    }
}
