using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class TransactionQueryServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TransactionQueryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetTransactionsByBroker_ReturnsAllTransactionsAcrossAssets()
    {
        var asset1 = MakeAsset("AAAA");
        asset1.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        var asset2 = MakeAsset("BBBB");
        asset2.AddTransaction(Transaction.Create(new DateTime(2026, 1, 6), Transaction.TransactionType.Sell, 5m, 20m, 0m));
        _repository.AssetsByBroker = [asset1, asset2];

        var result = CreateService().GetTransactionsByBroker("XPI");

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.AssetName == "AAAA" && t.Type == "Buy" && t.TotalPrice == 100m);
        result.Should().Contain(t => t.AssetName == "BBBB" && t.Type == "Sell" && t.TotalPrice == 100m);
    }

    [Fact]
    public void GetTransactionsByBroker_IncludesInactiveAssets()
    {
        var activeAsset = MakeAsset("AAAA");
        activeAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));

        var inactiveAsset = MakeAsset("BBBB");
        inactiveAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));
        inactiveAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 10m, 0m));
        _repository.AssetsByBroker = [activeAsset, inactiveAsset];

        var result = CreateService().GetTransactionsByBroker("XPI");

        result.Should().HaveCount(3);
        result.Should().Contain(t => t.AssetName == "BBBB");
    }

    [Fact]
    public void GetTransactionsByBroker_SortsByDateAscending()
    {
        var asset = MakeAsset("AAAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 3, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        _repository.AssetsByBroker = [asset];

        var result = CreateService().GetTransactionsByBroker("XPI");

        result.Select(t => t.Date).Should().BeInAscendingOrder();
        result[0].Date.Should().Be(new DateTime(2026, 1, 1));
        result[2].Date.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void GetTransactionsByBroker_SortsByAssetNameOnDateTie()
    {
        var sameDate = new DateTime(2026, 1, 5);
        var assetZ = MakeAsset("ZZZZ3");
        assetZ.AddTransaction(Transaction.Create(sameDate, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        var assetA = MakeAsset("AAAA3");
        assetA.AddTransaction(Transaction.Create(sameDate, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        _repository.AssetsByBroker = [assetZ, assetA];

        var result = CreateService().GetTransactionsByBroker("XPI");

        result[0].AssetName.Should().Be("AAAA3");
        result[1].AssetName.Should().Be("ZZZZ3");
    }

    [Fact]
    public void GetTransactionsByBroker_ReturnsEmptyForBrokerWithNoAssets()
    {
        var result = CreateService().GetTransactionsByBroker("UNKNOWN");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTransactionsByBroker_ReturnsEmptyOnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var asset = MakeAsset("AAAA");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        _repository.AssetsByBroker = [asset];

        var result = CreateService().GetTransactionsByBroker(brokerName!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTransactionsByPortfolio_ReturnsThatPortfoliosTransactions()
    {
        var asset = MakeAsset("AAAA");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];
        _repository.AssetsByBroker = [MakeAsset("SHOULD_NOT_APPEAR")];

        var result = CreateService().GetTransactionsByPortfolio("XPI", "Default");

        result.Should().ContainSingle(t => t.AssetName == "AAAA");
    }

    [Fact]
    public void GetTransactionsByPortfolio_ReturnsEmptyForUnknownPortfolio()
    {
        var result = CreateService().GetTransactionsByPortfolio("XPI", "UNKNOWN");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("   ", "Default")]
    [InlineData("XPI", null)]
    [InlineData("XPI", "")]
    [InlineData("XPI", "   ")]
    public void GetTransactionsByPortfolio_ReturnsEmptyOnNullOrWhitespaceParameters(string? brokerName, string? portfolioName)
    {
        var asset = MakeAsset("AAAA");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetTransactionsByPortfolio(brokerName!, portfolioName!);

        result.Should().BeEmpty();
    }

    private TransactionQueryService CreateService() => new(_repository);

    private static Asset MakeAsset(string name = "TEST") =>
        Asset.Create(name, "ISIN", "BVMF", name);

    private sealed class StubRepository : IRepository
    {
        public IEnumerable<Asset> AssetsByBroker { get; set; } = [];
        public IEnumerable<Asset> AssetsByBrokerPortfolio { get; set; } = [];
        public IEnumerable<Broker> Brokers { get; set; } = [];

        public IEnumerable<Asset> GetAssetsByBroker(string name) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio) => AssetsByBrokerPortfolio;
        public IEnumerable<Broker> GetBrokerList() => Brokers;
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
