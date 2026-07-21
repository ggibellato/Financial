using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class SummaryServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new SummaryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBrokerSummary_ReturnsSumOfBuyTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 15m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 20m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalBought.Should().Be(250m);
    }

    [Fact]
    public void GetBrokerSummary_ReturnsSumOfSellTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 12m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalSold.Should().Be(60m);
    }

    [Fact]
    public void GetBrokerSummary_ReturnsSumOfCredits()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 30m));
        asset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Rent, 15m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalCredits.Should().Be(45m);
    }

    [Fact]
    public void GetBrokerSummary_IncludesEveryAssetInScope_ScopePurityComesFromRepository()
    {
        var asset1 = MakeAsset();
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset1.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 20m));

        var zeroNetAsset = MakeZeroQuantityAsset();
        zeroNetAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 100m));

        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset1, zeroNetAsset)];

        var result = CreateService().GetBrokerSummary("XPI");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(100m);
        result.TotalSold.Should().Be(50m);
        result.TotalCredits.Should().Be(120m);
    }

    [Fact]
    public void GetBrokerSummary_ForwardsScopeToRepository()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        CreateService().GetBrokerSummary("XPI", InvestmentScope.Historic);

        _repository.LastRequestedBrokerListScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetBrokerSummary_ReturnsZerosForUnknownBrokerName()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        var result = CreateService().GetBrokerSummary("UNKNOWN");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(0m);
        result.TotalSold.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.TotalInvested.Should().Be(0m);
    }

    [Fact]
    public void GetBrokerSummary_IncludesEveryPortfolioRegardlessOfName()
    {
        var defaultAsset = MakeAsset("DEFAULT", "DEF");
        defaultAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var otherAsset = MakeAsset("CLOSED", "CLO");
        otherAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 100m, 5m, 0m));

        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(defaultAsset);
        broker.AddPortfolio("Encerradas").AddAsset(otherAsset);
        _repository.Brokers = [broker];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalBought.Should().Be(600m);
    }

    [Fact]
    public void GetBrokerSummary_TotalInvested_EqualsBoughtMinusSold()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 30m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 12m, 10m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalInvested.Should().Be(180m);
    }

    [Fact]
    public void GetBrokerSummary_TotalInvested_CanBeNegative()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalInvested.Should().Be(-150m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBrokerSummary_ReturnsZerosOnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var result = CreateService().GetBrokerSummary(brokerName!);

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(0m);
        result.TotalSold.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.TotalInvested.Should().Be(0m);
    }

    [Fact]
    public void GetPortfolioSummary_ReturnsSumOfBuyTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 8m, 25m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        result.TotalBought.Should().Be(200m);
    }

    [Fact]
    public void GetPortfolioSummary_ReturnsSumOfSellTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 3m, 15m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        result.TotalSold.Should().Be(45m);
    }

    [Fact]
    public void GetPortfolioSummary_ReturnsSumOfCredits()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 50m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        result.TotalCredits.Should().Be(50m);
    }

    [Fact]
    public void GetPortfolioSummary_IncludesEveryAssetInScope_ScopePurityComesFromRepository()
    {
        var asset1 = MakeAsset();
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));

        var zeroNetAsset = MakeZeroQuantityAsset();
        zeroNetAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 999m));

        _repository.AssetsByBrokerPortfolio = [asset1, zeroNetAsset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(100m);
        result.TotalCredits.Should().Be(999m);
    }

    [Fact]
    public void GetPortfolioSummary_ForwardsScopeToRepository()
    {
        _repository.AssetsByBrokerPortfolio = [MakeAsset()];

        CreateService().GetPortfolioSummary("XPI", "Default", InvestmentScope.Historic);

        _repository.LastRequestedAssetsByBrokerPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetPortfolioSummary_TotalInvested_EqualsBoughtMinusSold()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 10m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        result.TotalInvested.Should().Be(150m);
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("   ", "Default")]
    [InlineData("XPI", null)]
    [InlineData("XPI", "")]
    [InlineData("XPI", "   ")]
    public void GetPortfolioSummary_ReturnsZerosOnNullOrWhitespaceInput(string? brokerName, string? portfolioName)
    {
        var result = CreateService().GetPortfolioSummary(brokerName!, portfolioName!);

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(0m);
        result.TotalSold.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.TotalInvested.Should().Be(0m);
    }

    private SummaryService CreateService() => new(_repository);

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
        public InvestmentScope? LastRequestedBrokerListScope { get; private set; }
        public InvestmentScope? LastRequestedAssetsByBrokerPortfolioScope { get; private set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active)
        {
            LastRequestedAssetsByBrokerPortfolioScope = scope;
            return AssetsByBrokerPortfolio;
        }
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active)
        {
            LastRequestedBrokerListScope = scope;
            return Brokers;
        }
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
