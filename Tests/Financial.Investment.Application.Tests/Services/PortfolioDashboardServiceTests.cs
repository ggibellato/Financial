using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class PortfolioDashboardServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NextYear = new(2027, 1, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(Today.UtcDateTime);
    private static readonly DateTime PurchaseDate = new(2025, 1, 1);
    private static readonly DateTime SaleDate = new(2025, 6, 1);

    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<PortfolioDashboardService> _logger = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            null!, _tracer, _logger, TestHoldingValuationService.Create(), new StubExchangeRateProvider(null), new StubReportingCurrencyProvider(Currency.GBP));
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            _repository, null!, _logger, TestHoldingValuationService.Create(), new StubExchangeRateProvider(null), new StubReportingCurrencyProvider(Currency.GBP));
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            _repository, _tracer, null!, TestHoldingValuationService.Create(), new StubExchangeRateProvider(null), new StubReportingCurrencyProvider(Currency.GBP));
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullHoldingValuationService_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            _repository, _tracer, _logger, null!, new StubExchangeRateProvider(null), new StubReportingCurrencyProvider(Currency.GBP));
        act.Should().Throw<ArgumentNullException>().WithParameterName("holdingValuationService");
    }

    [Fact]
    public void Constructor_WithNullExchangeRateProvider_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            _repository, _tracer, _logger, TestHoldingValuationService.Create(), null!, new StubReportingCurrencyProvider(Currency.GBP));
        act.Should().Throw<ArgumentNullException>().WithParameterName("exchangeRateProvider");
    }

    [Fact]
    public void Constructor_WithNullReportingCurrencyProvider_Throws()
    {
        Action act = () => new PortfolioDashboardService(
            _repository, _tracer, _logger, TestHoldingValuationService.Create(), new StubExchangeRateProvider(null), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("reportingCurrencyProvider");
    }

    [Fact]
    public async Task GetDashboardAsync_SumsMarketValueAcrossActiveBrokersOnly()
    {
        SeedInvestments(
            active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m)), MakeBroker("T212", "GBP", PricedAsset("BBBB", 4m, 2m, 3m))],
            historic: [MakeBroker("XPI", "BRL", ClosedAsset("CLOSED"))]);

        var result = await CreateService().GetDashboardAsync();

        result.MarketValue.Should().Be(80m + 12m);
    }

    [Fact]
    public async Task GetDashboardAsync_InvestedIncludesUnpricedActiveHoldings()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m), BoughtAsset("BBBB", 4m, 2m))]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        result.Invested.Should().Be(50m + 8m);
        result.MarketValue.Should().Be(80m, "an unpriced holding contributes no market value");
    }

    [Fact]
    public async Task GetDashboardAsync_UnrealisedGainLoss_ExcludesUnpricedHoldingsCostBasis()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m), BoughtAsset("BBBB", 4m, 2m))]);

        var result = await CreateService().GetDashboardAsync();

        result.UnrealisedGainLoss.Should().Be(30m, "80 of priced market value less the 50 cost basis of that same priced holding");
    }

    [Fact]
    public async Task GetDashboardAsync_RealisedGainLoss_SumsActiveDisposalRecordsAcrossBothScopes()
    {
        SeedInvestments(
            active: [MakeBroker("XPI", "BRL", DisposedAsset("AAAA", 10m, 10m, 4m, 15m))],
            historic: [MakeBroker("XPI", "BRL", DisposedAsset("CLOSED", 5m, 10m, 5m, 12m))]);

        var result = await CreateService().GetDashboardAsync();

        result.RealisedGainLoss.Should().Be(20m + 10m);
    }

    [Fact]
    public async Task GetDashboardAsync_RealisedGainLoss_ExcludesSupersededDisposalRecords()
    {
        var asset = DisposedAsset("AAAA", 10m, 10m, 4m, 15m);
        asset.RecordTransaction(Transaction.Create(new DateTime(2025, 3, 1), Transaction.TransactionType.Buy, 10m, 20m, 0m));
        SeedInvestments(active: [MakeBroker("XPI", "BRL", asset)]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        asset.DisposalRecords.Should().Contain(record => record.Status == DisposalRecordStatus.Superseded);
        result.RealisedGainLoss.Should().Be(
            asset.DisposalRecords.Single(record => record.Status == DisposalRecordStatus.Active).GainLoss);
        result.RealisedGainLoss.Should().NotBe(20m, "the superseded record's own gain must not be counted as well");
    }

    [Fact]
    public async Task GetDashboardAsync_RealisedGainLoss_DoesNotDoubleCountCreditsViaAssetRealizedGainLoss()
    {
        var asset = DisposedAsset("AAAA", 10m, 10m, 4m, 15m);
        asset.AddCredit(Credit.Create(new DateTime(2025, 7, 1), Credit.CreditType.Dividend, 100m));
        SeedInvestments(active: [MakeBroker("XPI", "BRL", asset)]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        asset.RealizedGainLoss.Should().Be(120m, "the entity property folds the credit's gross value in");
        result.RealisedGainLoss.Should().Be(20m);
        result.IncomeLifetime.Should().Be(100m, "the credit is reported as income, once");
    }

    [Fact]
    public async Task GetDashboardAsync_IncomeYtd_SumsCreditsFromJanuaryFirstOfCurrentYear()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", AssetWithCredits())]);

        var result = await CreateService().GetDashboardAsync();

        result.IncomeYtd.Should().Be(40m, "only the current-year credit, net of what was withheld");
    }

    [Fact]
    public async Task GetDashboardAsync_IncomeYtd_ResetsAcrossNewCalendarYear()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", AssetWithCredits())]);

        var thisYear = await CreateService().GetDashboardAsync();
        var nextYear = await CreateService(new FakeTimeProvider(NextYear)).GetDashboardAsync();

        using var _ = new AssertionScope();
        thisYear.IncomeYtd.Should().Be(40m);
        nextYear.IncomeYtd.Should().Be(0m);
    }

    [Fact]
    public async Task GetDashboardAsync_IncomeLifetime_NeverAppliesADateFloor()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", AssetWithCredits())]);

        var thisYear = await CreateService().GetDashboardAsync();
        var nextYear = await CreateService(new FakeTimeProvider(NextYear)).GetDashboardAsync();

        using var _ = new AssertionScope();
        thisYear.IncomeLifetime.Should().Be(140m);
        nextYear.IncomeLifetime.Should().Be(140m);
    }

    [Fact]
    public async Task GetDashboardAsync_IncomeSpansHistoricHoldingsToo()
    {
        var closed = ClosedAsset("CLOSED");
        closed.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Dividend, 25m));
        SeedInvestments(
            active: [MakeBroker("XPI", "BRL", BoughtAsset("AAAA", 10m, 5m))],
            historic: [MakeBroker("XPI", "BRL", closed)]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        result.IncomeYtd.Should().Be(25m);
        result.IncomeLifetime.Should().Be(25m);
    }

    [Fact]
    public async Task GetDashboardAsync_IsPartial_TrueWhenAnyActiveHoldingHasNoMarketValue()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m), BoughtAsset("BBBB", 4m, 2m))]);

        var result = await CreateService().GetDashboardAsync();

        result.IsPartial.Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardAsync_IsPartial_FalseWhenEveryActiveHoldingIsPriced()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m), PricedAsset("BBBB", 4m, 2m, 3m))]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        result.IsPartial.Should().BeFalse();
        result.UnvaluedHoldingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardAsync_UnvaluedHoldingCount_CountsUnpricedActiveHoldingsOnly()
    {
        SeedInvestments(
            active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m), BoughtAsset("BBBB", 4m, 2m), BoughtAsset("CCCC", 3m, 7m))],
            historic: [MakeBroker("XPI", "BRL", ClosedAsset("CLOSED"))]);

        var result = await CreateService().GetDashboardAsync();

        result.UnvaluedHoldingCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_NetXirrIsBelowGrossXirrWhenIncomeWasWithheld()
    {
        var asset = PricedAsset("AAAA", 10m, 10m, 12m);
        asset.AddCredit(Credit.Create(SaleDate, Credit.CreditType.Dividend, 100m, withheld: 15m));
        SeedInvestments(active: [MakeBroker("XPI", "BRL", asset)]);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        result.GrossXirr.Should().NotBeNull();
        result.NetXirr.Should().NotBeNull();
        result.NetXirr.Should().BeLessThan(result.GrossXirr!.Value);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenReportingCurrencyDisabled_EveryConvertedFieldIsNull()
    {
        SeedInvestments(active: [MakeBroker("XPI", "BRL", PricedAsset("AAAA", 10m, 5m, 8m))]);
        var exchangeRateProvider = new StubExchangeRateProvider(0.2m);

        var result = await CreateService(
            exchangeRateProvider: exchangeRateProvider,
            reportingCurrencyProvider: new StubReportingCurrencyProvider(Currency.GBP, enabled: false)).GetDashboardAsync();

        using var _ = new AssertionScope();
        result.IsReportingCurrencyEnabled.Should().BeFalse();
        result.ReportingCurrency.Should().Be("GBP", "the configured setting still reports its value while off");
        result.ConvertedMarketValue.Should().BeNull();
        result.ConvertedInvested.Should().BeNull();
        result.ConvertedUnrealisedGainLoss.Should().BeNull();
        result.ConvertedRealisedGainLoss.Should().BeNull();
        result.ConvertedIncomeYtd.Should().BeNull();
        result.ConvertedIncomeLifetime.Should().BeNull();
        result.ConvertedGrossXirr.Should().BeNull();
        result.ConvertedNetXirr.Should().BeNull();
        result.IsReportingCurrencyPartial.Should().BeFalse();
        result.IsReportingCurrencyUnavailable.Should().BeFalse();
        exchangeRateProvider.CallCount.Should().Be(0);
        result.MarketValue.Should().Be(80m, "native figures are unaffected by the toggle");
    }

    [Fact]
    public async Task GetDashboardAsync_EmptyPortfolio_ReturnsZerosAndNoRate()
    {
        SeedInvestments(active: []);

        var result = await CreateService().GetDashboardAsync();

        using var _ = new AssertionScope();
        result.MarketValue.Should().Be(0m);
        result.Invested.Should().Be(0m);
        result.RealisedGainLoss.Should().Be(0m);
        result.IncomeLifetime.Should().Be(0m);
        result.GrossXirr.Should().BeNull();
        result.IsPartial.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardAsync_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetInvestments = new InvalidOperationException("simulated failure");

        Func<Task> act = () => CreateService().GetDashboardAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        using var _ = new AssertionScope();
        _tracer.Spans.Should().ContainSingle(span => span.RecordedException is InvalidOperationException);
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains("simulated failure"));
    }

    [Fact]
    public async Task GetDashboardAsync_WhenABrokerCurrencyIsUnrecognized_RecordsFailedSpanAndRethrows()
    {
        SeedInvestments(active: [MakeBroker("XPI", "XYZ", BoughtAsset("AAAA", 10m, 5m))]);

        Func<Task> act = () => CreateService().GetDashboardAsync();

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.Spans.Should().ContainSingle(span => span.RecordedException is ArgumentException);
    }

    private PortfolioDashboardService CreateService(
        TimeProvider? timeProvider = null,
        IExchangeRateProvider? exchangeRateProvider = null,
        IReportingCurrencyProvider? reportingCurrencyProvider = null)
    {
        var clock = timeProvider ?? new FakeTimeProvider(Today);
        return new PortfolioDashboardService(
            _repository,
            _tracer,
            _logger,
            TestHoldingValuationService.Create(clock),
            exchangeRateProvider ?? new StubExchangeRateProvider(null),
            reportingCurrencyProvider ?? new StubReportingCurrencyProvider(Currency.GBP),
            clock);
    }

    private void SeedInvestments(IReadOnlyList<Broker> active, IReadOnlyList<Broker>? historic = null)
    {
        var investments = Investments.Create();
        foreach (var broker in active)
        {
            investments.AddActiveBroker(broker);
        }

        foreach (var broker in historic ?? [])
        {
            investments.AddHistoricBroker(broker);
        }

        _repository.Investments = investments;
    }

    private static Broker MakeBroker(string name, string currency, params Asset[] assets)
    {
        var broker = Broker.Create(name, currency);
        var portfolio = broker.AddPortfolio("Default");
        foreach (var asset in assets)
        {
            portfolio.AddAsset(asset);
        }

        return broker;
    }

    private static Asset BoughtAsset(string name, decimal quantity, decimal unitPrice)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.AddTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, quantity, unitPrice, 0m));
        return asset;
    }

    private static Asset PricedAsset(string name, decimal quantity, decimal unitPrice, decimal price)
    {
        var asset = BoughtAsset(name, quantity, unitPrice);
        asset.SetPrice(TodayDate, price, isManual: false);
        return asset;
    }

    private static Asset ClosedAsset(string name)
    {
        var asset = BoughtAsset(name, quantity: 5m, unitPrice: 10m);
        asset.AddTransaction(Transaction.Create(SaleDate, Transaction.TransactionType.Sell, 5m, 12m, 0m));
        return asset;
    }

    private static Asset DisposedAsset(string name, decimal buyQuantity, decimal buyPrice, decimal sellQuantity, decimal sellPrice)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.RecordTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, buyQuantity, buyPrice, 0m));
        asset.RecordTransaction(Transaction.Create(SaleDate, Transaction.TransactionType.Sell, sellQuantity, sellPrice, 0m));
        return asset;
    }

    private static Asset AssetWithCredits()
    {
        var asset = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        asset.AddCredit(Credit.Create(new DateTime(2025, 12, 31), Credit.CreditType.Dividend, 100m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 3, 1), Credit.CreditType.Dividend, 50m, withheld: 10m));
        return asset;
    }
}
