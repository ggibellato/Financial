using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class BrokerBreakdownServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BrokerBreakdownService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Theory]
    [InlineData(InvestmentScope.Active)]
    [InlineData(InvestmentScope.Historic)]
    public void GetBrokerBreakdown_ReturnsAssetsWithinPortfolio(InvestmentScope scope)
    {
        var asset1 = MakeAsset("AAAA", "AAAA");
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var asset2 = MakeAsset("BBBB", "BBBB");
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset1, asset2)];

        var result = CreateService().GetBrokerBreakdown("XPI", scope);

        var portfolio = result.Single();
        portfolio.Assets.Should().HaveCount(2);
        portfolio.Assets.Should().Contain(a => a.AssetName == "AAAA" && a.TotalInvested == 100m);
        portfolio.Assets.Should().Contain(a => a.AssetName == "BBBB" && a.TotalInvested == 200m);
    }

    [Theory]
    [InlineData(InvestmentScope.Active)]
    [InlineData(InvestmentScope.Historic)]
    public void GetBrokerBreakdown_ExcludesAssetsWithNonPositiveTotalInvested(InvestmentScope scope)
    {
        var neverBoughtAsset = MakeAsset("NOBUY", "NOBUY");
        var boughtAsset = MakeAsset("BOUGHT", "BOUGHT");
        boughtAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", neverBoughtAsset, boughtAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI", scope);

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "BOUGHT");
    }

    [Theory]
    [InlineData(InvestmentScope.Active)]
    [InlineData(InvestmentScope.Historic)]
    public void GetBrokerBreakdown_SortsPortfoliosAlphabetically(InvestmentScope scope)
    {
        var zetaAsset = MakeAsset("ZETA-A", "ZETA-A");
        zetaAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var alphaAsset = MakeAsset("ALPHA-A", "ALPHA-A");
        alphaAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Zeta").AddAsset(zetaAsset);
        broker.AddPortfolio("Alpha").AddAsset(alphaAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI", scope);

        result.Select(p => p.PortfolioName).Should().Equal("Alpha", "Zeta");
    }

    [Theory]
    [InlineData(InvestmentScope.Active)]
    [InlineData(InvestmentScope.Historic)]
    public void GetBrokerBreakdown_ReturnsEmptyForUnknownBroker(InvestmentScope scope)
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        var result = CreateService().GetBrokerBreakdown("UNKNOWN", scope);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(InvestmentScope.Active, null)]
    [InlineData(InvestmentScope.Active, "")]
    [InlineData(InvestmentScope.Active, "   ")]
    [InlineData(InvestmentScope.Historic, null)]
    [InlineData(InvestmentScope.Historic, "")]
    [InlineData(InvestmentScope.Historic, "   ")]
    public void GetBrokerBreakdown_ReturnsEmptyOnNullOrWhitespaceBrokerName(InvestmentScope scope, string? brokerName)
    {
        var result = CreateService().GetBrokerBreakdown(brokerName!, scope);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBrokerBreakdown_DefaultScope_QueriesActiveScopeFromRepository()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        CreateService().GetBrokerBreakdown("XPI");

        _repository.LastRequestedScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void GetBrokerBreakdown_HistoricScope_QueriesHistoricScopeFromRepository()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Historic);

        _repository.LastRequestedScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetBrokerBreakdown_ActiveScope_UsesNetInvestedAsSelectedAmount()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 50m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Active);

        result.Should().ContainSingle(p => p.PortfolioName == "Default" && p.TotalInvested == 400m);
    }

    [Fact]
    public void GetBrokerBreakdown_ActiveScope_ExcludesAssetsWithNegativeNetInvested()
    {
        var positiveAsset = MakeAsset("POS", "POS");
        positiveAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var negativeAsset = MakeAsset("NEG", "NEG");
        negativeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        negativeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 100m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", positiveAsset, negativeAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Active);

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "POS");
    }

    [Fact]
    public void GetBrokerBreakdown_ActiveScope_ExcludesInactiveAssets()
    {
        var activeAsset = MakeAsset("ACTIVE", "ACTIVE");
        activeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var inactiveAsset = MakeZeroQuantityAsset();
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", activeAsset, inactiveAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Active);

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "ACTIVE");
    }

    [Fact]
    public void GetBrokerBreakdown_HistoricScope_UsesGrossTotalBoughtAsSelectedAmount()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 50m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Historic);

        result.Should().ContainSingle(p => p.PortfolioName == "Default" && p.TotalInvested == 500m);
    }

    [Fact]
    public void GetBrokerBreakdown_HistoricScope_IncludesFullyClosedPositionWithNonZeroTotalBought()
    {
        var closedAsset = MakeAsset("CLOSEDASSET", "CLOSEDASSET");
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 60m, 0m));
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", closedAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Historic);

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "CLOSEDASSET" && a.TotalInvested == 300m);
    }

    [Fact]
    public void GetBrokerBreakdown_DoesNotFilterByPortfolioName_ScopePurityComesFromRepository()
    {
        var defaultAsset = MakeAsset("DEFAULT", "DEF");
        defaultAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var otherAsset = MakeAsset("OTHER", "OTH");
        otherAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 100m, 5m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(defaultAsset);
        broker.AddPortfolio("Encerradas").AddAsset(otherAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().HaveCount(2);
        result.Select(p => p.PortfolioName).Should().Contain(["Default", "Encerradas"]);
    }

    [Fact]
    public void GetBrokerBreakdown_OmitsPortfolioWithNoQualifyingAssets()
    {
        var qualifyingAsset = MakeAsset("QUAL", "QUAL");
        qualifyingAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var inactiveAsset = MakeZeroQuantityAsset();

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(qualifyingAsset);
        broker.AddPortfolio("EmptyPortfolio").AddAsset(inactiveAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().ContainSingle();
        result.Single().PortfolioName.Should().Be("Default");
    }

    [Fact]
    public void GetBrokerBreakdown_ReturnsEmptyWhenNoEligiblePortfolios()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("EmptyPortfolio").AddAsset(MakeZeroQuantityAsset());
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBrokerBreakdown_SortsAssetsAlphabeticallyWithinPortfolio()
    {
        var zzzz = MakeAsset("ZZZZ3", "ZZZZ3");
        zzzz.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var aaaa = MakeAsset("AAAA3", "AAAA3");
        aaaa.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", zzzz, aaaa)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().Assets.Select(a => a.AssetName).Should().Equal("AAAA3", "ZZZZ3");
    }

    private BrokerBreakdownService CreateService() => new(_repository);

    private static Asset MakeAsset(string name = "TEST", string ticker = "TEST") =>
        Asset.Create(name, "ISIN", "BVMF", ticker);

    private static Asset MakeZeroQuantityAsset()
    {
        var asset = Asset.Create("INACTIVE", "ISIN2", "BVMF", "INACT");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 10m, 0m));
        return asset;
    }

    private static Broker MakeBrokerWithAssets(string brokerName, string portfolioName, params Asset[] assets)
    {
        var broker = Broker.Create(brokerName, "BRL");
        var portfolio = broker.AddPortfolio(portfolioName);
        foreach (var asset in assets)
        {
            portfolio.AddAsset(asset);
        }
        return broker;
    }

    private sealed class StubRepository : IRepository
    {
        public IEnumerable<Asset> AssetsByBroker { get; set; } = [];
        public IEnumerable<Asset> AssetsByBrokerPortfolio { get; set; } = [];
        public IEnumerable<Broker> Brokers { get; set; } = [];
        public InvestmentScope? LastRequestedScope { get; private set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => AssetsByBrokerPortfolio;
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active)
        {
            LastRequestedScope = scope;
            return Brokers;
        }
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
