using Financial.Investment.Application.Services;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
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
        result.Currency.Should().Be("BRL");
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
