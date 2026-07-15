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

    [Fact]
    public void GetBrokerBreakdown_ReturnsPortfolioWithTotalInvested()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 50m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 10m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().ContainSingle(p => p.PortfolioName == "Default" && p.TotalInvested == 400m);
    }

    [Fact]
    public void GetBrokerBreakdown_ReturnsAssetsWithinPortfolio()
    {
        var asset1 = MakeAsset("AAAA", "AAAA");
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var asset2 = MakeAsset("BBBB", "BBBB");
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset1, asset2)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        var portfolio = result.Single();
        portfolio.Assets.Should().HaveCount(2);
        portfolio.Assets.Should().Contain(a => a.AssetName == "AAAA" && a.TotalInvested == 100m);
        portfolio.Assets.Should().Contain(a => a.AssetName == "BBBB" && a.TotalInvested == 200m);
    }

    [Fact]
    public void GetBrokerBreakdown_PortfolioTotalInvested_EqualsSumOfIncludedAssets()
    {
        var includedAsset1 = MakeAsset("AAAA", "AAAA");
        includedAsset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var includedAsset2 = MakeAsset("BBBB", "BBBB");
        includedAsset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        var excludedAsset = MakeAsset("CCCC", "CCCC");
        excludedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        excludedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 100m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", includedAsset1, includedAsset2, excludedAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().TotalInvested.Should().Be(300m);
    }

    [Fact]
    public void GetBrokerBreakdown_ExcludesAssetsWithNonPositiveTotalInvested()
    {
        var positiveAsset = MakeAsset("POS", "POS");
        positiveAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var negativeAsset = MakeAsset("NEG", "NEG");
        negativeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        negativeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 100m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", positiveAsset, negativeAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "POS");
    }

    [Fact]
    public void GetBrokerBreakdown_ExcludesInactiveAssets()
    {
        var activeAsset = MakeAsset("ACTIVE", "ACTIVE");
        activeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var inactiveAsset = MakeZeroQuantityAsset();
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", activeAsset, inactiveAsset)];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Single().Assets.Should().ContainSingle(a => a.AssetName == "ACTIVE");
    }

    [Fact]
    public void GetBrokerBreakdown_ExcludesEncerradasPortfolio()
    {
        var defaultAsset = MakeAsset("DEFAULT", "DEF");
        defaultAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var closedAsset = MakeAsset("CLOSED", "CLO");
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 100m, 5m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(defaultAsset);
        broker.AddPortfolio("Encerradas").AddAsset(closedAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().ContainSingle();
        result.Single().PortfolioName.Should().Be("Default");
    }

    [Theory]
    [InlineData("Encerradas")]
    [InlineData("encerradas")]
    [InlineData("ENCERRADAS")]
    [InlineData(" Encerradas ")]
    public void GetBrokerBreakdown_ExcludesEncerradasPortfolio_CaseInsensitive(string portfolioName)
    {
        var closedAsset = MakeAsset("CLOSED", "CLO");
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 100m, 5m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio(portfolioName).AddAsset(closedAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

        result.Should().BeEmpty();
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

    [Fact]
    public void GetBrokerBreakdown_ReturnsEmptyForUnknownBroker()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        var result = CreateService().GetBrokerBreakdown("UNKNOWN");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBrokerBreakdown_ReturnsEmptyWhenNoEligiblePortfolios()
    {
        var closedAsset = MakeAsset("CLOSED", "CLO");
        closedAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 100m, 5m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Encerradas").AddAsset(closedAsset);
        broker.AddPortfolio("EmptyPortfolio").AddAsset(MakeZeroQuantityAsset());
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerBreakdown("XPI");

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

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => AssetsByBrokerPortfolio;
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => Brokers;
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
