using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Application.Tests.Services;

public class PortfolioAssetSummaryServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new PortfolioAssetSummaryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsAssetClass_MatchingAssetClassification()
    {
        var asset = Asset.Create("Bitcoin", "", "", "BTC", CountryCode.UK, "", GlobalAssetClass.Cryptocurrency);
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("Coinbase", "Cryptocurrency");

        result.Should().HaveCount(1);
        result[0].Class.Should().Be(GlobalAssetClass.Cryptocurrency);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsSingleAsset_WithCorrectFields()
    {
        var asset = MakeAsset("ALZR11", "ALZR11", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.Should().HaveCount(1);
        var item = result[0];
        item.AssetName.Should().Be("ALZR11");
        item.Ticker.Should().Be("ALZR11");
        item.Exchange.Should().Be("BVMF");
        item.CurrentQuantity.Should().Be(20m);
        item.AveragePrice.Should().Be(100m);
        item.TotalBought.Should().Be(2500m);
        item.TotalSold.Should().Be(550m);
        item.TotalInvested.Should().Be(1950m);
        item.PortfolioWeight.Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SortsAlphabetically()
    {
        _repository.Assets = [
            MakeAsset("ZEBRA", "ZBR", "BVMF"),
            MakeAsset("APPLE", "APL", "BVMF"),
            MakeAsset("MANGO", "MNG", "BVMF"),
        ];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Select(i => i.AssetName).Should().Equal("APPLE", "MANGO", "ZEBRA");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ComputesPortfolioWeight()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 300m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 700m, 0m));

        _repository.Assets = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.First(i => i.AssetName == "ALPHA").PortfolioWeight.Should().Be(30m);
        result.First(i => i.AssetName == "BETA").PortfolioWeight.Should().Be(70m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsZeroPortfolioWeight_WhenAllTotalInvestedAreZero()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 100m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 200m, 0m));
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 200m, 0m));

        _repository.Assets = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().AllSatisfy(i => i.PortfolioWeight.Should().Be(0m));
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SetsFirstInvestmentDate_FromEarliestBuyTransaction()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 15), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].FirstInvestmentDate.Should().Be(new DateTime(2020, 1, 15));
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SetsFirstInvestmentDate_Null_WhenNoBuyTransactions()
    {
        _repository.Assets = [MakeAsset("TEST", "TST", "BVMF")];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].FirstInvestmentDate.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_IncludesAllAssets_RegardlessOfActiveStatus()
    {
        var active = MakeAsset("ACTIVE", "ACT", "BVMF");
        active.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var inactive = MakeAsset("INACTIVE", "INA", "BVMF");
        inactive.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        inactive.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 10m, 0m));

        _repository.Assets = [active, inactive];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_WhenPortfolioHasNoAssets()
    {
        _repository.Assets = [];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_OnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var result = CreateService().GetPortfolioAssetsSummary(brokerName!, "Default");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_OnNullOrWhitespacePortfolioName(string? portfolioName)
    {
        var result = CreateService().GetPortfolioAssetsSummary("XPI", portfolioName!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsTotalCredits_SumOfAllCreditValues()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2023, 1, 1), Credit.CreditType.Dividend, 30m));
        asset.AddCredit(Credit.Create(new DateTime(2023, 6, 1), Credit.CreditType.Rent, 15m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].TotalCredits.Should().Be(45m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsTotalCredits_Zero_WhenNoCredits()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].TotalCredits.Should().Be(0m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_BuyEntriesAreNegative()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result[0].CashFlows.Should().HaveCount(1);
        result[0].CashFlows[0].Amount.Should().Be(-100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_SellEntriesArePositive()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 10m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        var sellFlow = result[0].CashFlows.First(f => f.Amount > 0 && f.Date.Year == 2022);
        sellFlow.Amount.Should().Be(50m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_CreditEntriesArePositive()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2023, 3, 1), Credit.CreditType.Dividend, 25m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        var creditFlow = result[0].CashFlows.First(f => f.Date.Year == 2023);
        creditFlow.Amount.Should().Be(25m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_SortedAscendingByDate()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 6, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 9, 15), Credit.CreditType.Dividend, 5m));
        asset.AddTransaction(Transaction.Create(new DateTime(2023, 1, 1), Transaction.TransactionType.Sell, 1m, 12m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        var dates = result[0].CashFlows.Select(f => f.Date).ToList();
        dates.Should().BeInAscendingOrder();
        dates[0].Should().Be(new DateTime(2021, 9, 15));
        dates[1].Should().Be(new DateTime(2022, 6, 1));
        dates[2].Should().Be(new DateTime(2023, 1, 1));
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_Empty_WhenNoTransactionsOrCredits()
    {
        _repository.Assets = [MakeAsset("TEST", "TST", "BVMF")];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CashFlows.Should().NotBeNull();
        result[0].CashFlows.Should().BeEmpty();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_ContainsAllThreeEventTypes()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 2m, 12m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 8m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result[0].CashFlows.Should().HaveCount(3);
        result[0].CashFlows.Count(f => f.Amount < 0).Should().Be(1);
        result[0].CashFlows.Count(f => f.Amount > 0).Should().Be(2);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCashFlows_BuyAmountMatchesTotalPrice()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 15m, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CashFlows[0].Amount.Should().Be(-155m);
    }

    // ── Phase 3: Credits-analysis unit tests ─────────────────────────────────

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastMonthCredits_SumOfMostRecentMonthCredits()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 1000m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 20m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 10m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 15), Credit.CreditType.Dividend, 8m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastMonthCredits.Should().Be(18m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastMonthCredits_Zero_WhenNoCredits()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 1000m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastMonthCredits.Should().Be(0m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastCreditMonth_FormattedYYYYMM()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 10), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastCreditMonth.Should().Be("2024-06");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastCreditMonth_Null_WhenNoCredits()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastCreditMonth.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ExcludesFutureCreditDatesFromLastMonthCalculation()
    {
        var pastDate = DateTime.Today.AddMonths(-1);
        var futureDate = DateTime.Today.AddDays(5);
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(pastDate, Credit.CreditType.Dividend, 10m));
        asset.AddCredit(Credit.Create(futureDate, Credit.CreditType.Dividend, 99m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result[0].LastCreditMonth.Should().Be($"{pastDate.Year:D4}-{pastDate.Month:D2}");
        result[0].LastMonthCredits.Should().Be(10m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastMonthCreditsPercent_Computed()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 1000m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 10m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastMonthCreditsPercent.Should().Be(1m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastMonthCreditsPercent_Null_WhenTotalInvestedIsZero()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 2, 1), Transaction.TransactionType.Sell, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastMonthCreditsPercent.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsLastMonthCreditsPercent_Null_WhenNoCredits()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].LastMonthCreditsPercent.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_DetectsMonthlyFrequency()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 15), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CreditFrequencyPerYear.Should().Be(12);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_DetectsQuarterlyFrequency()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 4, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 7, 15), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CreditFrequencyPerYear.Should().Be(4);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_DetectsFourMonthFrequency()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 5, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 9, 15), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CreditFrequencyPerYear.Should().Be(3);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsNullFrequency_WhenOnlyOneDistinctCreditMonth()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 15), Credit.CreditType.Dividend, 3m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CreditFrequencyPerYear.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsNullFrequency_WhenGapOutsideKnownRanges()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 15), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 9, 15), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CreditFrequencyPerYear.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEstimatedAnnualCredits_Computed()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 10000m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 50m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 50m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 50m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].EstimatedAnnualCredits.Should().Be(600m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEstimatedAnnualCredits_Null_WhenFrequencyNull()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].EstimatedAnnualCredits.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEstimatedAnnualPercent_Computed()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 10000m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 50m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 50m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 50m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].EstimatedAnnualPercent.Should().Be(6m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEstimatedAnnualPercent_Null_WhenEstimatedCreditsNull()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].EstimatedAnnualPercent.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEstimatedAnnualPercent_Null_WhenTotalInvestedIsZero()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 2, 1), Transaction.TransactionType.Sell, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 5m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].EstimatedAnnualPercent.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCurrentMonthCredits_SumOfCurrentMonthCredits()
    {
        var today = DateTime.Today;
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(today.Year, today.Month, 1), Credit.CreditType.Dividend, 12m));
        asset.AddCredit(Credit.Create(new DateTime(today.Year, today.Month, 10), Credit.CreditType.Dividend, 8m));
        asset.AddCredit(Credit.Create(today.AddMonths(-1), Credit.CreditType.Dividend, 99m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CurrentMonthCredits.Should().Be(20m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsCurrentMonthCredits_Zero_WhenNoneInCurrentMonth()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(DateTime.Today.AddMonths(-1), Credit.CreditType.Dividend, 15m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].CurrentMonthCredits.Should().Be(0m);
    }

    private PortfolioAssetSummaryService CreateService() => new(_repository);

    private static Asset MakeAsset(string name, string ticker, string exchange) =>
        Asset.Create(name, "ISIN", exchange, ticker);

    private sealed class StubRepository : IRepository
    {
        public IEnumerable<Asset> Assets { get; set; } = [];

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => [];
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => Assets;
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => [];
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
