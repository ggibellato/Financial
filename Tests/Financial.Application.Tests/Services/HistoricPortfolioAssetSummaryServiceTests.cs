using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Application.Tests.Services;

public class HistoricPortfolioAssetSummaryServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new HistoricPortfolioAssetSummaryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_WeightsByGrossTotalBought()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 60m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        using var _ = new AssertionScope();
        result.Should().HaveCount(1);
        result[0].TotalBought.Should().Be(300m);
        result[0].TotalSold.Should().Be(250m);
        result[0].PortfolioWeight.Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ComputesRealizedGainLoss()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 60m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 50m, 0m));
        asset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 20m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        // RealizedGainLoss = TotalSold - TotalBought + TotalCredits = 250 - 300 + 20 = -30
        result[0].RealizedGainLoss.Should().Be(-30m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_PortfolioWeightsSumTo100Percent()
    {
        var asset1 = MakeAsset("ALPHA", "ALP", "BVMF");
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 300m, 0m));
        asset1.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 290m, 0m));

        var asset2 = MakeAsset("BETA", "BET", "BVMF");
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 700m, 0m));
        asset2.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 690m, 0m));

        _repository.Assets = [asset1, asset2];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        using var _ = new AssertionScope();
        result.First(i => i.AssetName == "ALPHA").PortfolioWeight.Should().Be(30m);
        result.First(i => i.AssetName == "BETA").PortfolioWeight.Should().Be(70m);
        result.Sum(i => i.PortfolioWeight).Should().Be(100m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_CreditPercentFields_UseGrossTotalBoughtBasis()
    {
        var asset = MakeAsset("CLOSEDASSET", "CLOSEDASSET", "BVMF");
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 1000m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 6, 1), Transaction.TransactionType.Sell, 1m, 990m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2020, 3, 1), Credit.CreditType.Dividend, 10m));
        _repository.Assets = [asset];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        // Net invested would be 1000 - 990 = 10, making 10/10*100 = 100%; gross bought basis gives 10/1000*100 = 1%
        result[0].LastMonthCreditsPercent.Should().Be(1m);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_QueriesHistoricScopeFromRepository()
    {
        _repository.Assets = [MakeAsset("TEST", "TST", "BVMF")];

        CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        _repository.LastRequestedScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_WhenPortfolioHasNoAssets()
    {
        _repository.Assets = [];

        var result = CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPortfolioAssetsSummary_ReturnsEmptyList_OnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var result = CreateService().GetPortfolioAssetsSummary(brokerName!, "Uncategorized");

        result.Should().BeEmpty();
    }

    private HistoricPortfolioAssetSummaryService CreateService() => new(_repository);

    private static Asset MakeAsset(string name, string ticker, string exchange) =>
        Asset.Create(name, "ISIN", exchange, ticker);

    private sealed class StubRepository : IRepository
    {
        public IEnumerable<Asset> Assets { get; set; } = [];
        public InvestmentScope? LastRequestedScope { get; private set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => [];
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active)
        {
            LastRequestedScope = scope;
            return Assets;
        }
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => [];
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => null;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
