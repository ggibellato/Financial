using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class BrokerHoldingsArcaTests
{
    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void GetBrokerHoldings_BrokerWithBuckets_OrdersPortfoliosByArcaSectionThenName()
    {
        _repository.Brokers = [MakeArcaBroker(withNotInArca: true)];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Groups.Select(g => g.Name).Should().Equal("Acoes", "FII", "Renda Fixa", "Previdencia", "Reserva", "Outros");
        result.Groups.Select(g => g.ArcaBucket).Should().Equal(
            ArcaBucket.Shares, ArcaBucket.RealEstate, ArcaBucket.Cash, ArcaBucket.Pension, ArcaBucket.ReserveFund, null);
        result.Groups.Select(g => g.ArcaSection).Should().Equal(
            ArcaSection.Arca, ArcaSection.Arca, ArcaSection.Arca, ArcaSection.Arca, ArcaSection.ReserveFund, ArcaSection.NotInArca);
    }

    [Fact]
    public void GetBrokerHoldings_BrokerWithBuckets_TotalsArcaAndTheGrandTotalApart()
    {
        _repository.Brokers = [MakeArcaBroker()];

        var result = CreateService().GetBrokerHoldings("XPI");

        var arca = result.Arca!;
        using var _ = new AssertionScope();
        arca.ArcaTotal.MarketValue.Should().Be(1000m);
        arca.ArcaTotal.MarketShareOfBase.Should().Be(100m);
        arca.Buckets.Single(b => b.Bucket == ArcaBucket.RealEstate).Totals.MarketShareOfBase.Should().Be(30m);
        arca.ReserveFund!.MarketValue.Should().Be(250m);
        arca.NotInArca.Should().BeNull();
        arca.ArcaAndReserveFundTotal.MarketValue.Should().Be(1250m);
        result.Total.MarketValue.Should().Be(1250m);
    }

    [Fact]
    public void GetBrokerHoldings_BrokerWithBuckets_ListsOnlyAssignedBucketsInArcaOrderAddingUpToOneHundred()
    {
        var broker = MakeArcaBroker();
        broker.Portfolios.Single(p => p.Name == "Renda Fixa").SetArcaBucket(null);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        var buckets = result.Arca!.Buckets;
        using var _ = new AssertionScope();
        buckets.Select(b => b.Bucket).Should().Equal(ArcaBucket.Shares, ArcaBucket.RealEstate, ArcaBucket.Pension);
        buckets.Sum(b => b.Totals.MarketShareOfBase!.Value).Should().BeApproximately(100m, 0.01m);
    }

    [Fact]
    public void GetBrokerHoldings_BrokerWithBuckets_SharesUseTheArcaTotalAndLeaveOtherSectionsEmpty()
    {
        _repository.Brokers = [MakeArcaBroker(withNotInArca: true)];

        var result = CreateService().GetBrokerHoldings("XPI");

        var fii = result.Groups.Single(g => g.Name == "FII");
        var reserve = result.Groups.Single(g => g.Name == "Reserva");
        var other = result.Groups.Single(g => g.Name == "Outros");
        using var _ = new AssertionScope();
        fii.Holdings.Single().MarketShareOfBase.Should().Be(30m);
        fii.Subtotal.InvestedShareOfBase.Should().Be(30m);
        reserve.Holdings.Single().MarketShareOfBase.Should().BeNull();
        reserve.Subtotal.MarketShareOfBase.Should().BeNull();
        other.Holdings.Single().InvestedShareOfBase.Should().BeNull();
        result.Total.MarketShareOfBase.Should().BeNull();
        result.Arca!.NotInArca!.MarketValue.Should().Be(50m);
        result.Arca.ArcaAndReserveFundTotal.MarketValue.Should().Be(1250m);
        result.Total.MarketValue.Should().Be(1300m);
    }

    [Fact]
    public void GetBrokerHoldings_ArcaTotalOfZero_LeavesEveryArcaShareEmpty()
    {
        var broker = Broker.Create("XPI", "BRL");
        var shares = broker.AddPortfolio("Acoes");
        shares.SetArcaBucket(ArcaBucket.Shares);
        shares.AddAsset(MakeAsset("UNVL3", units: 2m));
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Arca!.ArcaTotal.MarketValue.Should().BeNull();
        result.Arca.ArcaTotal.MarketShareOfBase.Should().BeNull();
        result.Arca.Buckets.Single().Totals.MarketShareOfBase.Should().BeNull();
        result.Groups.Single().Holdings.Single().MarketShareOfBase.Should().BeNull();
    }

    [Fact]
    public void GetBrokerHoldings_BrokerWithoutBuckets_KeepsTheBrokerLayout()
    {
        _repository.Brokers = [ClearBuckets(MakeArcaBroker())];

        var result = CreateService().GetBrokerHoldings("XPI");

        using var _ = new AssertionScope();
        result.Arca.Should().BeNull();
        result.Groups.Should().AllSatisfy(g => g.ArcaSection.Should().BeNull());
        result.Groups.Select(g => g.Name).Should().BeInAscendingOrder(StringComparer.CurrentCultureIgnoreCase);
        result.Total.MarketShareOfBase.Should().Be(100m);
    }

    [Fact]
    public void GetBrokerHoldings_OnlyAnEmptyPortfolioHasABucket_KeepsTheBrokerLayout()
    {
        var broker = ClearBuckets(MakeArcaBroker());
        broker.AddPortfolio("Nova").SetArcaBucket(ArcaBucket.Pension);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        result.Arca.Should().BeNull();
    }

    [Fact]
    public void GetBrokerHoldings_SegmentedArcaPortfolio_KeepsItsSegmentGroups()
    {
        var broker = MakeArcaBroker();
        var logistics = Asset.Create("LOGI11", "ISIN", "BVMF", "LOGI11", CountryCode.BR, "FII", GlobalAssetClass.RealEstate);
        logistics.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        logistics.SetPrice(TestClock.Today, 100m, isManual: false);
        logistics.SetSegment("Logistics");
        broker.Portfolios.Single(p => p.Name == "FII").AddAsset(logistics);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerHoldings("XPI");

        var fii = result.Groups.Single(g => g.Name == "FII");
        using var _ = new AssertionScope();
        fii.Segments.Select(s => s.Name).Should().Equal("Logistics", null);
        fii.Segments.Sum(s => s.Subtotal.MarketValue).Should().Be(fii.Subtotal.MarketValue);
        fii.Segments[0].Subtotal.MarketShareOfBase.Should().BeApproximately(100m / 11m, 0.0001m);
    }

    [Fact]
    public void GetBrokerArca_ReturnsTheAllocationAndTheHeldCurrencies()
    {
        _repository.Brokers = [MakeArcaBroker()];

        var result = CreateService().GetBrokerArca("XPI");

        using var _ = new AssertionScope();
        result.Currencies.Should().Equal("BRL");
        result.Arca!.ArcaTotal.MarketValue.Should().Be(1000m);
    }

    [Fact]
    public void GetBrokerArca_BrokerWithoutBuckets_ReturnsNoAllocation()
    {
        _repository.Brokers = [ClearBuckets(MakeArcaBroker())];

        CreateService().GetBrokerArca("XPI").Arca.Should().BeNull();
    }

    private static Broker ClearBuckets(Broker broker)
    {
        foreach (var portfolio in broker.Portfolios)
        {
            portfolio.SetArcaBucket(null);
        }
        return broker;
    }

    private PortfolioAssetSummaryService CreateService() =>
        new(_repository, TestHoldingValuationService.Create(), TestClock.At(), new RecordingTelemetryTracer(), NullLogger<PortfolioAssetSummaryService>.Instance);

    private static Broker MakeArcaBroker(bool withNotInArca = false)
    {
        var broker = Broker.Create("XPI", "BRL");
        AddPortfolio(broker, "Reserva", ArcaBucket.ReserveFund, 250m);
        AddPortfolio(broker, "Previdencia", ArcaBucket.Pension, 400m);
        AddPortfolio(broker, "Renda Fixa", ArcaBucket.Cash, 100m);
        AddPortfolio(broker, "FII", ArcaBucket.RealEstate, 300m);
        AddPortfolio(broker, "Acoes", ArcaBucket.Shares, 200m);
        if (withNotInArca)
        {
            AddPortfolio(broker, "Outros", null, 50m);
        }
        return broker;
    }

    private static void AddPortfolio(Broker broker, string name, ArcaBucket? bucket, decimal value)
    {
        var portfolio = broker.AddPortfolio(name);
        portfolio.SetArcaBucket(bucket);
        portfolio.AddAsset(MakeAsset($"{name}-1", units: 1m, price: value));
    }

    private static Asset MakeAsset(string name, decimal units, decimal? price = null)
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name);
        asset.AddTransaction(Transaction.Create(TestClock.LocalToday, Transaction.TransactionType.Buy, units, price ?? 100m, 0m));
        if (price.HasValue)
        {
            asset.SetPrice(TestClock.Today, price.Value, isManual: false);
        }
        return asset;
    }
}
