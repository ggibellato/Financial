using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class PortfolioAssetSummaryServiceTests
{
    private readonly RecordingTelemetryTracer _tracer = new();

    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsAssetClass_MatchingAssetClassification()
    {
        var asset = Asset.Create("Bitcoin", "", "", "BTC", CountryCode.UK, "", GlobalAssetClass.Cryptocurrency);
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("Coinbase", "Cryptocurrency");

        result.Should().HaveCount(1);
        result[0].Class.Should().Be(GlobalAssetClass.Cryptocurrency);
        _tracer.ShouldHaveSucceededSpan("Investment.PortfolioAssetSummaryService.GetPortfolioAssetsSummary");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsSingleAsset_WithCorrectFields()
    {
        var asset = MakeAsset("ALZR11", "ALZR11", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.SetPrice(TestClock.Today, 120m, isManual: false);
        _repository.AssetsByBrokerPortfolio = [asset];

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
        item.TotalInvested.Should().Be(2000m);
        item.RealizedGainLoss.Should().Be(50m);
        item.PortfolioWeight.Should().Be(100m);
        item.MarketStatus.Should().Be(Financial.Investment.Domain.Rules.MarketStatus.Current);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SortsAlphabetically()
    {
        _repository.AssetsByBrokerPortfolio = [
            MakeAsset("ZEBRA", "ZBR", "BVMF"),
            MakeAsset("APPLE", "APL", "BVMF"),
            MakeAsset("MANGO", "MNG", "BVMF"),
        ];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Select(i => i.AssetName).Should().Equal("APPLE", "MANGO", "ZEBRA");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_ComputesPortfolioWeightFromMarketValue()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 300m, 0m));
        asset1.SetPrice(TestClock.Today, 300m, isManual: false);

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 700m, 0m));
        asset2.SetPrice(TestClock.Today, 700m, isManual: false);

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.First(i => i.AssetName == "ALPHA").PortfolioWeight.Should().Be(30m);
        result.First(i => i.AssetName == "BETA").PortfolioWeight.Should().Be(70m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_AppreciatedHoldingsShareExceedsItsCostBasedShare()
    {
        var appreciated = MakeAsset("APPRECIATED", "APR", "BVMF");
        appreciated.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        appreciated.SetPrice(TestClock.Today, 400m, isManual: false);

        var flat = MakeAsset("FLAT", "FLT", "BVMF");
        flat.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        flat.SetPrice(TestClock.Today, 100m, isManual: false);

        _repository.AssetsByBrokerPortfolio = [appreciated, flat];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        // Cost-based share would be 50%/50% (both cost 100); market-based share is 80%/20% (400 vs 100).
        result.First(i => i.AssetName == "APPRECIATED").PortfolioWeight.Should().Be(80m);
        result.First(i => i.AssetName == "FLAT").PortfolioWeight.Should().Be(20m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_UnpricedHolding_ReportsUnknownShareNotZero()
    {
        var priced = MakeAsset("PRICED", "PRD", "BVMF");
        priced.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        priced.SetPrice(TestClock.Today, 100m, isManual: false);

        var unpriced = MakeAsset("UNPRICED", "UNP", "BVMF");
        unpriced.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 500m, 0m));

        _repository.AssetsByBrokerPortfolio = [priced, unpriced];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.First(i => i.AssetName == "UNPRICED").PortfolioWeight.Should().BeNull();
        result.First(i => i.AssetName == "PRICED").PortfolioWeight.Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_AllHoldingsUnpriced_EveryShareIsUnknown()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 300m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 700m, 0m));

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().AllSatisfy(i => i.PortfolioWeight.Should().BeNull());
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_IncomeYieldPercentagesStayOnCost_UnaffectedByMarketValue()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.SetPrice(TestClock.Today, 100000m, isManual: false);
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 10m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        // Yield on cost (10/100*100 = 10%) must not silently follow the market-value weight basis.
        result[0].LastMonthCreditsPercent.Should().Be(10m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsZeroPortfolioWeight_WhenAllTotalInvestedAreZero()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 1m, 100m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 200m, 0m));
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 1m, 200m, 0m));

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().AllSatisfy(i => i.PortfolioWeight.Should().Be(0m));
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SetsFirstInvestmentDate_FromEarliestBuyTransaction()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 15), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].FirstInvestmentDate.Should().Be(new DateTime(2020, 1, 15));
    }

    [Fact]
    public void GetPortfolioAssetsSummary_SetsFirstInvestmentDate_Null_WhenNoBuyTransactions()
    {
        _repository.AssetsByBrokerPortfolio = [MakeAsset("TEST", "TST", "BVMF")];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result[0].FirstInvestmentDate.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_IncludesAllAssets_RegardlessOfActiveStatus()
    {
        var active = MakeAsset("ACTIVE", "ACT", "BVMF");
        active.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var inactive = MakeAsset("INACTIVE", "INA", "BVMF");
        inactive.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        inactive.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 5m, 10m, 0m));

        _repository.AssetsByBrokerPortfolio = [active, inactive];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_WhenPortfolioHasNoAssets()
    {
        _repository.AssetsByBrokerPortfolio = [];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        result.Should().BeEmpty();
        _tracer.ShouldHaveSucceededSpan("Investment.PortfolioAssetSummaryService.GetPortfolioAssetsSummary");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_OnNullOrWhitespaceBrokerName(string? brokerName)
    {
        _repository.AssetsByBrokerPortfolio = [MakeAsset("AAAA", "AAAA", "BVMF")];

        var result = CreateService().GetPortfolioAssetsSummary(brokerName!, "Default");

        result.Should().BeEmpty();
        _tracer.ShouldHaveSucceededSpan("Investment.PortfolioAssetSummaryService.GetPortfolioAssetsSummary");
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
    public void GetPortfolioAssetsSummary_ActiveScope_FullySoldHolding_LeavesBothIncomeYieldPercentagesEmpty()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 2, 1), Transaction.TransactionType.Sell, 1m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 3, 1), Credit.CreditType.Dividend, 5m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var item = CreateService().GetPortfolioAssetsSummary("XPI", "Default").Single();

        using var _ = new AssertionScope();
        item.EstimatedAnnualCredits.Should().NotBeNull();
        item.LastMonthCreditsPercent.Should().BeNull("a fully sold holding has no open cost to yield on");
        item.EstimatedAnnualPercent.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_MapsTheCreditsAnalysisAndTheAssetsOwnTotalsOntoTheItem()
    {
        var asset = MakeAsset("TEST", "TST", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 15), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2026, 3, 1), Transaction.TransactionType.Sell, 2m, 15m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 4, 2), Credit.CreditType.Dividend, 6m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 7, 2), Credit.CreditType.Dividend, 8m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 10, 1), Credit.CreditType.Dividend, 5m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 11, 3), Credit.CreditType.Dividend, 7m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var item = CreateService().GetPortfolioAssetsSummary("XPI", "Default").Single();

        using var _ = new AssertionScope();
        item.TotalCredits.Should().Be(26m);
        item.LastMonthCredits.Should().Be(5m);
        item.LastCreditMonth.Should().Be("2026-10");
        item.LastMonthCreditsPercent.Should().Be(6.25m);
        item.CreditFrequencyPerYear.Should().Be(4);
        item.EstimatedAnnualCredits.Should().Be(20m);
        item.EstimatedAnnualPercent.Should().Be(25m);
        item.CurrentMonthCredits.Should().Be(5m);
        item.RealizedGainLoss.Should().Be(36m);
        item.RealizedGainLossSharesOnly.Should().Be(10m);
        item.AverageSellPrice.Should().Be(15m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_HistoricScope_WeightsByGrossTotalBought()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 5m, 60m, 0m));
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized", InvestmentScope.Historic);

        using var _ = new AssertionScope();
        result.Should().HaveCount(1);
        result[0].TotalBought.Should().Be(300m);
        result[0].TotalSold.Should().Be(250m);
        result[0].PortfolioWeight.Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_HistoricScope_PortfolioWeightsSumTo100Percent()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 300m, 0m));
        asset1.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 1m, 290m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 700m, 0m));
        asset2.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 1m, 690m, 0m));

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized", InvestmentScope.Historic);

        using var _ = new AssertionScope();
        result.First(i => i.AssetName == "ALPHA").PortfolioWeight.Should().Be(30m);
        result.First(i => i.AssetName == "BETA").PortfolioWeight.Should().Be(70m);
        result.Sum(i => i.PortfolioWeight).Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_HistoricScope_CreditPercentFields_UseGrossTotalBoughtBasis()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 1000m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 6, 1), Transaction.TransactionType.Sell, 1m, 990m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2020, 3, 1), Credit.CreditType.Dividend, 10m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized", InvestmentScope.Historic);

        // Net invested would be 1000 - 990 = 10, making 10/10*100 = 100%; gross bought basis gives 10/1000*100 = 1%
        result[0].LastMonthCreditsPercent.Should().Be(1m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ActiveScope_ReturnsTheResultPercentOnTheCostOfUnitsHeld()
    {
        var asset = MakeAsset("OPEN", "OPN", "BVMF");
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 2m, 100m, 0m));
        asset.AddCredit(Credit.Create(TestClock.LocalToday, Credit.CreditType.Dividend, 20m));
        asset.SetPrice(TestClock.Today, 120m, isManual: false);
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result[0].ResultPercent.Should().Be(20m);
        result[0].ResultWithCreditsPercent.Should().Be(30m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_HistoricScope_ReturnsTheRealisedResultPercentOnTotalBought()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 4m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 4m, 105m, 0m));
        asset.AddCredit(Credit.Create(TestClock.LocalToday, Credit.CreditType.Dividend, 20m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized", InvestmentScope.Historic);

        using var _ = new AssertionScope();
        result[0].ResultPercent.Should().Be(5m);
        result[0].ResultWithCreditsPercent.Should().Be(10m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetAssetsByBrokerPortfolio = new InvalidOperationException("simulated failure");

        var act = () => CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.PortfolioAssetSummaryService.GetPortfolioAssetsSummary");
    }

    private PortfolioAssetSummaryService CreateService() => new(_repository, TestHoldingValuationService.Create(), TestClock.At(), _tracer, NullLogger<PortfolioAssetSummaryService>.Instance);

    private static Asset MakeAsset(string name, string ticker, string exchange) =>
        Asset.Create(name, "ISIN", exchange, ticker);
}
