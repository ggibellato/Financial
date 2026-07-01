using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Application.Tests.Services;

public class SummaryQueryServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new SummaryQueryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBrokerSummary_ReturnsSumOfBuyTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 15m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 20m, 0m));
        _repository.AssetsByBroker = [asset];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalBought.Should().Be(250m);
    }

    [Fact]
    public void GetBrokerSummary_ReturnsSumOfSellTransactions()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 12m, 0m));
        _repository.AssetsByBroker = [asset];

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
        _repository.AssetsByBroker = [asset];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalCredits.Should().Be(45m);
    }

    [Fact]
    public void GetBrokerSummary_ExcludesInactiveAssets()
    {
        var activeAsset = MakeAsset();
        activeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 5m, 0m));
        activeAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 20m));

        var inactiveAsset = MakeZeroQuantityAsset();
        inactiveAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 100m));

        _repository.AssetsByBroker = [activeAsset, inactiveAsset];

        var result = CreateService().GetBrokerSummary("XPI");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(50m);
        result.TotalCredits.Should().Be(20m);
    }

    [Fact]
    public void GetBrokerSummary_ReturnsZerosForBrokerWithNoActiveAssets()
    {
        _repository.AssetsByBroker = [MakeZeroQuantityAsset()];

        var result = CreateService().GetBrokerSummary("XPI");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(0m);
        result.TotalSold.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
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
    public void GetPortfolioSummary_ExcludesInactiveAssets()
    {
        var activeAsset = MakeAsset();
        activeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));

        var inactiveAsset = MakeZeroQuantityAsset();
        inactiveAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 999m));

        _repository.AssetsByBrokerPortfolio = [activeAsset, inactiveAsset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(50m);
        result.TotalCredits.Should().Be(0m);
    }

    [Fact]
    public void GetPortfolioSummary_ReturnsZerosForPortfolioWithNoActiveAssets()
    {
        _repository.AssetsByBrokerPortfolio = [MakeZeroQuantityAsset()];

        var result = CreateService().GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(0m);
        result.TotalSold.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
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
    }

    private SummaryQueryService CreateService() => new(_repository);

    private static Asset MakeAsset(string name = "TEST", string ticker = "TEST") =>
        Asset.Create(name, "ISIN", "BVMF", ticker);

    private static Asset MakeZeroQuantityAsset()
    {
        var asset = Asset.Create("INACTIVE", "ISIN2", "BVMF", "INACT");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 10m, 0m));
        return asset;
    }

    private sealed class StubRepository : IRepository
    {
        public IEnumerable<Asset> AssetsByBroker { get; set; } = [];
        public IEnumerable<Asset> AssetsByBrokerPortfolio { get; set; } = [];

        public IEnumerable<Asset> GetAssetsByBroker(string name) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio) => AssetsByBrokerPortfolio;
        public IEnumerable<Broker> GetBrokerList() => [];
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
