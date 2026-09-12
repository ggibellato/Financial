using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

public class SummaryServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new SummaryService(null!, Tracer, NullLogger<SummaryService>.Instance, TestHoldingValuationService.Create(), new XirrCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new SummaryService(new StubInvestmentRepository(), null!, NullLogger<SummaryService>.Instance, TestHoldingValuationService.Create(), new XirrCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullHoldingValuationService_Throws()
    {
        Action act = () => new SummaryService(new StubInvestmentRepository(), Tracer, NullLogger<SummaryService>.Instance, null!, new XirrCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("holdingValuationService");
    }

    [Fact]
    public void Constructor_WithNullXirrCalculationService_Throws()
    {
        Action act = () => new SummaryService(new StubInvestmentRepository(), Tracer, NullLogger<SummaryService>.Instance, TestHoldingValuationService.Create(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("xirrCalculationService");
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
        asset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.SecuritiesLendingIncome, 15m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalCredits.Should().Be(45m);
    }

    [Fact]
    public void GetBrokerSummary_ActiveScope_IncludesAssetClosedToZeroQuantity()
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
        result.TotalInvested.Should().Be(50m);
    }

    [Fact]
    public void GetBrokerSummary_HistoricScope_IncludesZeroQuantityAssetTotals()
    {
        var zeroNetAsset = MakeZeroQuantityAsset();
        zeroNetAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 100m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", zeroNetAsset)];

        var result = CreateService().GetBrokerSummary("XPI", InvestmentScope.Historic);

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(50m);
        result.TotalSold.Should().Be(50m);
        result.TotalCredits.Should().Be(100m);
        result.TotalInvested.Should().Be(50m);
    }

    [Fact]
    public void GetBrokerSummary_ForwardsScopeToRepository()
    {
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", MakeAsset())];

        CreateService().GetBrokerSummary("XPI", InvestmentScope.Historic);

        _repository.LastGetBrokerListScope.Should().Be(InvestmentScope.Historic);
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
        result.MarketValue.Should().Be(0m);
        result.HoldingCount.Should().Be(0);
        result.UnvaluedHoldingCount.Should().Be(0);
        result.PriceOnlyReturn.Should().BeNull();
        result.TotalReturn.Should().BeNull();
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
    public void GetBrokerSummary_ActiveScope_TotalInvested_EqualsCostOfUnitsCurrentlyHeld()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 30m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 12m, 50m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalInvested.Should().Be(180m);
    }

    [Fact]
    public void GetBrokerSummary_ActiveScope_TotalInvested_ClampsToZeroWhenPositionIsOversold()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 15m, 50m, 0m));
        _repository.Brokers = [MakeBrokerWithAssets("XPI", "Default", asset)];

        var result = CreateService().GetBrokerSummary("XPI");

        asset.Quantity.Should().Be(-5m);
        result.TotalInvested.Should().Be(0m);
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
        result.MarketValue.Should().Be(0m);
        result.HoldingCount.Should().Be(0);
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
    public void GetPortfolioSummary_ActiveScope_IncludesAssetClosedToZeroQuantity()
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
        result.TotalInvested.Should().Be(50m);
    }

    [Fact]
    public void GetPortfolioSummary_HistoricScope_IncludesZeroQuantityAssetTotals()
    {
        var zeroNetAsset = MakeZeroQuantityAsset();
        zeroNetAsset.AddCredit(Credit.Create(DateTime.Today, Credit.CreditType.Dividend, 999m));
        _repository.AssetsByBrokerPortfolio = [zeroNetAsset];

        var result = CreateService().GetPortfolioSummary("XPI", "Default", InvestmentScope.Historic);

        using var _ = new AssertionScope();
        result.TotalBought.Should().Be(50m);
        result.TotalSold.Should().Be(50m);
        result.TotalCredits.Should().Be(999m);
        result.TotalInvested.Should().Be(50m);
    }

    [Fact]
    public void GetPortfolioSummary_ForwardsScopeToRepository()
    {
        _repository.AssetsByBrokerPortfolio = [MakeAsset()];

        CreateService().GetPortfolioSummary("XPI", "Default", InvestmentScope.Historic);

        _repository.LastGetAssetsByBrokerPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetPortfolioSummary_ActiveScope_TotalInvested_EqualsCostOfUnitsCurrentlyHeld()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 20m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 5m, 40m, 0m));
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
        result.MarketValue.Should().Be(0m);
        result.HoldingCount.Should().Be(0);
    }

    private SummaryService CreateService(TimeProvider? timeProvider = null) =>
        new(_repository, Tracer, NullLogger<SummaryService>.Instance, TestHoldingValuationService.Create(timeProvider), new XirrCalculationService(), timeProvider);

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


    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new SummaryService(new StubInvestmentRepository(), Tracer, null!, TestHoldingValuationService.Create(), new XirrCalculationService());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetBrokerSummary_WhenRepositoryThrowsUnexpectedly_Rethrows()
    {
        _repository.ThrowOnGetBrokerList = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetBrokerSummary("XPI");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetPortfolioSummary_WhenRepositoryThrowsUnexpectedly_Rethrows()
    {
        _repository.ThrowOnGetAssetsByBrokerPortfolio = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetPortfolioSummary("XPI", "Default");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetPortfolioSummary_FullyValued_MarketValueIsSumAndReturnsComputed()
    {
        var asset1 = MakeAsset("AAAA", "AAAA");
        asset1.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset1.SetPrice(DateOnly.FromDateTime(Today.UtcDateTime), 8m, isManual: false);

        var asset2 = MakeAsset("BBBB", "BBBB");
        asset2.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 4m, 2m, 0m));
        asset2.SetPrice(DateOnly.FromDateTime(Today.UtcDateTime), 3m, isManual: false);

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService(new FakeTimeProvider(Today)).GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.HoldingCount.Should().Be(2);
        result.UnvaluedHoldingCount.Should().Be(0);
        result.MarketValue.Should().Be(80m + 12m);
        result.PriceOnlyReturn.Should().NotBeNull();
        result.TotalReturn.Should().NotBeNull();
    }

    [Fact]
    public void GetPortfolioSummary_PartiallyValued_MarketValueIsSumOfValuedRowsAndReturnsWithheld()
    {
        var priced = MakeAsset("AAAA", "AAAA");
        priced.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        priced.SetPrice(DateOnly.FromDateTime(Today.UtcDateTime), 8m, isManual: false);

        var unpriced = MakeAsset("BBBB", "BBBB");
        unpriced.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 4m, 2m, 0m));

        _repository.AssetsByBrokerPortfolio = [priced, unpriced];

        var result = CreateService(new FakeTimeProvider(Today)).GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.HoldingCount.Should().Be(2);
        result.UnvaluedHoldingCount.Should().Be(1);
        result.MarketValue.Should().Be(80m);
        result.PriceOnlyReturn.Should().BeNull();
        result.TotalReturn.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioSummary_NothingValuable_MarketValueAndReturnsAreNull()
    {
        var asset1 = MakeAsset("AAAA", "AAAA");
        asset1.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var asset2 = MakeAsset("BBBB", "BBBB");
        asset2.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 4m, 2m, 0m));

        _repository.AssetsByBrokerPortfolio = [asset1, asset2];

        var result = CreateService(new FakeTimeProvider(Today)).GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.HoldingCount.Should().Be(2);
        result.UnvaluedHoldingCount.Should().Be(2);
        result.MarketValue.Should().BeNull();
        result.PriceOnlyReturn.Should().BeNull();
        result.TotalReturn.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioSummary_ZeroQuantityHoldingWithNoPrice_CountsAsValuedNotUnvalued()
    {
        var zeroNetAsset = MakeZeroQuantityAsset();

        _repository.AssetsByBrokerPortfolio = [zeroNetAsset];

        var result = CreateService(new FakeTimeProvider(Today)).GetPortfolioSummary("XPI", "Default");

        using var _ = new AssertionScope();
        result.HoldingCount.Should().Be(1);
        result.UnvaluedHoldingCount.Should().Be(0);
        result.MarketValue.Should().Be(0m);
    }

    [Fact]
    public void GetBrokerSummary_NeverSumsAcrossBrokers()
    {
        var xpiAsset = MakeAsset("AAAA", "AAAA");
        xpiAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var otherAsset = MakeAsset("BBBB", "BBBB");
        otherAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1000m, 1000m, 0m));

        _repository.Brokers =
        [
            MakeBrokerWithAssets("XPI", "Default", xpiAsset),
            MakeBrokerWithAssets("OTHER", "Default", otherAsset),
        ];

        var result = CreateService().GetBrokerSummary("XPI");

        result.TotalBought.Should().Be(50m);
    }
}
