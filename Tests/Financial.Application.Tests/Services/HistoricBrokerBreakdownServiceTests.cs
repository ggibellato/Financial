using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class HistoricBrokerBreakdownServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new HistoricBrokerBreakdownService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBrokerBreakdown_UsesGrossTotalBoughtAsSelectedAmount()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 50m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Uncategorized", asset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().ContainSingle(p => p.PortfolioName == "Uncategorized" && p.TotalInvested == 500m);
    }

    [Fact]
    public void GetBrokerBreakdown_IncludesFullyClosedPositionWithNonZeroTotalBought()
    {
        var closedAsset = MakeAsset("CLOSEDASSET", "CLOSEDASSET");
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 60m, 0m));
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Uncategorized", closedAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "CLOSEDASSET" && a.TotalInvested == 300m);
    }

    [Fact]
    public void GetBrokerBreakdown_ExcludesAssetsWithNonPositiveTotalInvested()
    {
        var neverBoughtAsset = MakeAsset("NOBUY", "NOBUY");
        var boughtAsset = MakeAsset("BOUGHT", "BOUGHT");
        boughtAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Uncategorized", neverBoughtAsset, boughtAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "BOUGHT");
    }

    [Fact]
    public void GetBrokerBreakdown_SortsPortfoliosAlphabetically()
    {
        var zetaAsset = MakeAsset("ZETA-A", "ZETA-A");
        zetaAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var alphaAsset = MakeAsset("ALPHA-A", "ALPHA-A");
        alphaAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Zeta").AddAsset(zetaAsset);
        broker.AddPortfolio("Alpha").AddAsset(alphaAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Select(p => p.PortfolioName).Should().Equal("Alpha", "Zeta");
    }

    [Fact]
    public void GetBrokerBreakdown_QueriesHistoricScopeFromRepository()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Uncategorized", MakeAsset())];

        CreateService().GetBrokerBreakdown("XPI");

        _repository.LastRequestedScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetBrokerBreakdown_ReturnsEmptyForUnknownBroker()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Uncategorized", MakeAsset())];

        var result = CreateService().GetBrokerBreakdown("UNKNOWN");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBrokerBreakdown_ReturnsEmptyOnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var result = CreateService().GetBrokerBreakdown(brokerName!);

        result.Should().BeEmpty();
    }

    private HistoricBrokerBreakdownService CreateService() => new(_repository);

    private static Asset MakeAsset(string name = "TEST", string ticker = "TEST") =>
        Asset.Create(name, "ISIN", "BVMF", ticker);

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
