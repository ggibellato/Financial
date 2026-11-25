using Financial.Investment.Application.Services;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class BrokerHoldingsTests
{
    private const string SpanName = "Investment.PortfolioAssetSummaryService.GetBrokerHoldings";

    private readonly RecordingTelemetryTracer _tracer = new();

    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void GetBrokerHoldings_GroupsHoldingsByPortfolioInAlphabeticalOrder()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Currencies.Should().Equal("BRL");
        result.Groups.Select(g => g.Name).Should().Equal("Acoes", "FII");
        result.Groups[1].Holdings.Select(h => h.Asset.AssetName).Should().Equal("AAAA", "BBBB");
        _tracer.ShouldHaveSucceededSpan(SpanName);
    }

    [Fact]
    public void GetBrokerHoldings_SharesUseTheGroupAndBrokerTotals()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var holding = result.Groups[1].Holdings[0];
        using var _ = new AssertionScope();
        holding.InvestedShareOfGroup.Should().Be(25m);
        holding.InvestedShareOfBase.Should().Be(10m);
        holding.MarketShareOfGroup.Should().BeApproximately(21.4286m, 0.0001m);
        holding.MarketShareOfBase.Should().BeApproximately(21.4286m, 0.0001m);
    }

    [Fact]
    public void GetBrokerHoldings_SingleHoldingGroup_LeavesTheGroupSharesEmpty()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var holding = result.Groups[0].Holdings.Single();
        using var _ = new AssertionScope();
        holding.InvestedShareOfGroup.Should().BeNull();
        holding.MarketShareOfGroup.Should().BeNull();
        holding.InvestedShareOfBase.Should().Be(60m);
        holding.MarketShareOfBase.Should().BeNull();
    }

    [Fact]
    public void GetBrokerHoldings_LeavesOutHoldingsWithNoQuantity()
    {
        var sold = MakeAsset("SOLD", units: 5m);
        sold.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Sell, 5m, 100m, 0m));
        var broker = MakeSampleBroker();
        broker.Portfolios.Single(p => p.Name == "FII").AddAsset(sold);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        result.Groups[1].Holdings.Select(h => h.Asset.AssetName).Should().Equal("AAAA", "BBBB");
    }

    [Fact]
    public void GetBrokerHoldings_SubtotalSumsTheGroupAndItsProfitOverValuedHoldings()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var subtotal = result.Groups[1].Subtotal;
        using var _ = new AssertionScope();
        subtotal.HoldingCount.Should().Be(2);
        subtotal.UnvaluedHoldingCount.Should().Be(0);
        subtotal.Invested.Should().Be(4000m);
        subtotal.MarketValue.Should().Be(4200m);
        subtotal.InvestedShareOfBase.Should().Be(40m);
        subtotal.MarketShareOfBase.Should().Be(100m);
        subtotal.ProfitAmount.Should().Be(200m);
        subtotal.ProfitPercent.Should().Be(5m);
        subtotal.ProfitWithCreditsPercent.Should().Be(5.825m);
        subtotal.TotalCredits.Should().Be(33m);
        subtotal.CreditsPercentOfInvested.Should().Be(0.825m);
        subtotal.LastCreditAmount.Should().Be(33m);
        subtotal.LastCreditPercentOfMarketValue.Should().BeApproximately(0.7857m, 0.0001m);
    }

    [Fact]
    public void GetBrokerHoldings_BrokerTotalCountsTheHoldingsThatCouldNotBeValued()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var total = result.Total;
        using var _ = new AssertionScope();
        total.HoldingCount.Should().Be(3);
        total.UnvaluedHoldingCount.Should().Be(1);
        total.Invested.Should().Be(10000m);
        total.MarketValue.Should().Be(4200m);
        total.ProfitAmount.Should().Be(200m);
        total.ProfitPercent.Should().Be(5m);
        result.Groups[0].Subtotal.MarketValue.Should().BeNull();
        result.Groups[0].Subtotal.ProfitAmount.Should().BeNull();
    }

    [Fact]
    public void GetBrokerHoldings_HoldingsBoughtInSeveralCurrencies_ListsEachCurrency()
    {
        var broker = MakeSampleBroker();
        var dollarAsset = Asset.Create("USDA", "ISIN", "NYSE", "USDA");
        dollarAsset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 10m, 0m, currency: Currency.USD));
        broker.Portfolios.Single(p => p.Name == "FII").AddAsset(dollarAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        result.Currencies.Should().Equal("BRL", "USD");
    }

    [Fact]
    public void GetBrokerHoldings_UnvaluedHoldingWithALastCredit_LeavesTheLastCreditPercentEmpty()
    {
        var broker = MakeSampleBroker();
        broker.Portfolios.Single(p => p.Name == "Acoes").Assets.Single()
            .AddCredit(Credit.Create(TestClock.LocalToday, Credit.CreditType.Dividend, 12m));
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Total.LastCreditAmount.Should().Be(45m);
        result.Total.LastCreditPercentOfMarketValue.Should().BeNull();
        result.Groups[1].Subtotal.LastCreditPercentOfMarketValue.Should().NotBeNull();
    }

    [Fact]
    public void GetBrokerHoldings_UnknownBroker_ReturnsNoGroups()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("Nowhere");

        result.Groups.Should().BeEmpty();
        _tracer.ShouldHaveSucceededSpan(SpanName);
    }

    [Fact]
    public void GetBrokerHoldings_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetBrokerList = new InvalidOperationException("simulated failure");

        var act = () => CreateService().GetBrokerHoldings("XPI");

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>(SpanName);
    }

    [Fact]
    public void GetBrokerHoldings_PortfolioWithoutSegments_HasNoSegmentGroups()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        result.Groups.Should().AllSatisfy(g => g.Segments.Should().BeEmpty());
    }

    [Fact]
    public void GetBrokerHoldings_SegmentedPortfolio_GroupsBySegmentAlphabeticallyWithNoSegmentLast()
    {
        _repository.Brokers = [MakeSegmentedBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var group = result.Groups.Single();
        using var _ = new AssertionScope();
        group.Segments.Select(s => s.Name).Should().Equal("Logistics", "Paper", null);
        group.Segments[0].Holdings.Select(h => h.Asset.AssetName).Should().Equal("LOGA11", "LOGB11");
        group.Segments[2].Holdings.Select(h => h.Asset.AssetName).Should().Equal("NONE11");
        group.Holdings.Should().HaveCount(4);
    }

    [Fact]
    public void GetBrokerHoldings_SegmentsDifferingOnlyInCaseAndSpaces_ShareOneGroupNamedAfterTheFirstHolding()
    {
        _repository.Brokers = [MakeSegmentedBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var logistics = result.Groups.Single().Segments[0];
        logistics.Name.Should().Be("Logistics");
        logistics.Holdings.Select(h => h.Asset.Segment).Should().Equal("Logistics", "logistics");
    }

    [Fact]
    public void GetBrokerHoldings_SegmentSubtotal_ShowsItsShareOfThePortfolioAndAddsUpToThePortfolioSubtotal()
    {
        _repository.Brokers = [MakeSegmentedBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var group = result.Groups.Single();
        var logistics = group.Segments[0];
        using var _ = new AssertionScope();
        logistics.Subtotal.Invested.Should().Be(3000m);
        logistics.InvestedShareOfGroup.Should().Be(60m);
        logistics.MarketShareOfGroup.Should().BeApproximately(54.5455m, 0.0001m);
        logistics.Subtotal.InvestedShareOfBase.Should().Be(60m);
        group.Segments.Sum(s => s.Subtotal.Invested).Should().Be(group.Subtotal.Invested);
        group.Segments.Sum(s => s.Subtotal.MarketValue).Should().Be(group.Subtotal.MarketValue);
        group.Segments.Sum(s => s.Subtotal.TotalCredits).Should().Be(group.Subtotal.TotalCredits);
    }

    [Fact]
    public void GetBrokerHoldings_HoldingShareOfGroup_StaysRelativeToThePortfolio()
    {
        _repository.Brokers = [MakeSegmentedBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var holding = result.Groups.Single().Segments[0].Holdings[0];
        holding.InvestedShareOfGroup.Should().Be(20m);
    }

    [Fact]
    public void GetBrokerHoldings_Rows_CarryTheSegmentAndPickSource()
    {
        _repository.Brokers = [MakeSegmentedBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var holding = result.Groups.Single().Segments[1].Holdings.Single().Asset;
        holding.Segment.Should().Be("Paper");
        holding.PickSource.Should().Be(PickSource.Recommended);
    }

    [Fact]
    public void GetBrokerHoldings_NamesTheStartOfLastMonthAndOfThisMonthWithTheirPriceWindows()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.LastMonthStart.Date.Should().Be(new DateOnly(2026, 9, 1));
        result.LastMonthStart.EarliestPriceDate.Should().Be(new DateOnly(2026, 8, 25));
        result.ThisMonthStart.Date.Should().Be(new DateOnly(2026, 10, 1));
        result.ThisMonthStart.EarliestPriceDate.Should().Be(new DateOnly(2026, 9, 24));
    }

    [Fact]
    public void GetBrokerHoldings_UnknownBroker_StillNamesTheMonthStarts()
    {
        var result = CreateService().GetBrokerHoldings("Nowhere");

        result.ThisMonthStart.Date.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void GetBrokerHoldings_Row_ShowsBothMonthStartValuesTheirPricesAndTheChange()
    {
        _repository.Brokers = [MakeMonthStartBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var row = result.Groups.Single().Holdings.Single(h => h.Asset.AssetName == "BOTH11");
        using var _ = new AssertionScope();
        row.LastMonthStart.Value.Should().Be(1000m);
        row.LastMonthStart.UnitPrice.Should().Be(10m);
        row.LastMonthStart.PriceDate.Should().Be(new DateOnly(2026, 9, 1));
        row.ThisMonthStart.Value.Should().Be(1100m);
        row.ThisMonthStart.PriceDate.Should().Be(new DateOnly(2026, 9, 30));
        row.MonthChangePercent.Should().Be(10m);
        row.ThisMonthStart.Currency.Should().Be("BRL");
    }

    [Fact]
    public void GetBrokerHoldings_Subtotal_SumsOnlyHoldingsWithBothMonthStartValuesAndCountsTheOthers()
    {
        _repository.Brokers = [MakeMonthStartBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var subtotal = result.Groups.Single().Subtotal;
        using var _ = new AssertionScope();
        subtotal.LastMonthStartValue.Should().Be(1000m);
        subtotal.ThisMonthStartValue.Should().Be(1100m);
        subtotal.MonthChangePercent.Should().Be(10m);
        subtotal.MonthStartExcludedCount.Should().Be(1);
        result.Total.MonthStartExcludedCount.Should().Be(1);
    }

    [Fact]
    public void GetBrokerHoldings_NoHoldingWithBothMonthStartValues_LeavesTheSubtotalSumsEmpty()
    {
        _repository.Brokers = [MakeSampleBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Total.LastMonthStartValue.Should().BeNull();
        result.Total.MonthChangePercent.Should().BeNull();
        result.Total.MonthStartExcludedCount.Should().Be(3);
    }

    [Fact]
    public void GetBrokerHoldings_DoesNotSaveAnything()
    {
        _repository.Brokers = [MakeMonthStartBroker()];

        CreateService().GetBrokerHoldings("XPI");

        _repository.WriteCallCount.Should().Be(0);
    }

    private PortfolioAssetSummaryService CreateService() =>
        new(_repository, TestHoldingValuationService.Create(), TestClock.At(), _tracer, NullLogger<PortfolioAssetSummaryService>.Instance);

    private static Broker MakeSampleBroker()
    {
        var broker = Broker.Create("XPI", "BRL");
        var fii = broker.AddPortfolio("FII");
        var second = MakeAsset("BBBB", units: 30m, price: 110m);
        second.AddCredit(Credit.Create(TestClock.LocalToday, Credit.CreditType.Dividend, 33m));
        fii.AddAsset(second);
        fii.AddAsset(MakeAsset("AAAA", units: 10m, price: 90m));
        broker.AddPortfolio("Acoes").AddAsset(MakeAsset("CCCC", units: 60m));
        return broker;
    }

    private static Broker MakeMonthStartBroker()
    {
        var broker = Broker.Create("XPI", "BRL");
        var fii = broker.AddPortfolio("FII");
        var both = MakeAsset("BOTH11", units: 100m);
        both.SetPrice(new DateOnly(2026, 9, 1), 10m, isManual: false);
        both.SetPrice(new DateOnly(2026, 9, 30), 11m, isManual: false);
        fii.AddAsset(both);
        var onlyThisMonth = MakeAsset("ONLY11", units: 10m);
        onlyThisMonth.SetPrice(new DateOnly(2026, 10, 1), 50m, isManual: false);
        fii.AddAsset(onlyThisMonth);
        return broker;
    }

    private static Broker MakeSegmentedBroker()
    {
        var broker = Broker.Create("XPI", "BRL");
        var fii = broker.AddPortfolio("FII");
        fii.AddAsset(MakeRealEstateAsset("LOGA11", 10m, 100m, "Logistics"));
        fii.AddAsset(MakeRealEstateAsset("LOGB11", 20m, 100m, " logistics "));
        var paper = MakeRealEstateAsset("PAPR11", 10m, 150m, "Paper");
        paper.SetPickSource(PickSource.Recommended);
        fii.AddAsset(paper);
        fii.AddAsset(MakeRealEstateAsset("NONE11", 10m, 100m, null));
        return broker;
    }

    private static Asset MakeRealEstateAsset(string name, decimal units, decimal price, string? segment)
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name, CountryCode.BR, "FII", GlobalAssetClass.RealEstate);
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, units, 100m, 0m));
        asset.SetPrice(TestClock.Today, price, isManual: false);
        asset.SetSegment(segment);
        return asset;
    }

    private static Asset MakeAsset(string name, decimal units, decimal? price = null)
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name);
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, units, 100m, 0m));
        if (price.HasValue)
        {
            asset.SetPrice(TestClock.Today, price.Value, isManual: false);
        }
        return asset;
    }
}
