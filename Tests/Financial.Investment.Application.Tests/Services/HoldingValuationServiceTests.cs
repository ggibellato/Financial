using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

public class HoldingValuationServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private static Asset MakeAsset(string name = "AAAA") => Asset.Create(name, "ISIN", "BVMF", name);

    private HoldingValuationService CreateService(ITelemetryTracer? tracer = null, TimeProvider? timeProvider = null) =>
        new(new XirrCalculationService(), tracer ?? new RecordingTelemetryTracer(), NullLogger<HoldingValuationService>.Instance, timeProvider ?? new FakeTimeProvider(Today));

    [Fact]
    public void Constructor_WithNullXirrCalculationService_Throws()
    {
        Action act = () => new HoldingValuationService(null!, new RecordingTelemetryTracer(), NullLogger<HoldingValuationService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("xirrCalculationService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new HoldingValuationService(new XirrCalculationService(), null!, NullLogger<HoldingValuationService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new HoldingValuationService(new XirrCalculationService(), new RecordingTelemetryTracer(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GetValuation_NullAsset_Throws()
    {
        var service = CreateService();

        Action act = () => service.GetValuation(null!, InvestmentScope.Active);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetValuation_ActiveWithNoRecordedPrice_ReportsMarketValueUnavailable()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Active);

        using var _ = new AssertionScope();
        result.MarketValue.Should().BeNull();
        result.UnrealisedGain.Should().BeNull();
        result.PriceAsOfDate.Should().BeNull();
        result.PriceOnlyReturn.Should().BeNull();
        result.TotalReturn.Should().BeNull();
        result.CostOfUnitsHeld.Should().Be(50m);
    }

    [Fact]
    public void GetValuation_ActiveWithRecordedPrice_ReportsMarketValueAndUnrealisedGain()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.SetPrice(new DateOnly(2026, 8, 14), 8m, isManual: false);
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Active);

        using var _ = new AssertionScope();
        result.MarketValue.Should().Be(80m);
        result.UnrealisedGain.Should().Be(30m);
        result.PriceAsOfDate.Should().Be(new DateOnly(2026, 8, 14));
        result.IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void GetValuation_ActiveWithRecordedPrice_ComputesPriceOnlyAndTotalReturn()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 5m));
        asset.SetPrice(new DateOnly(2026, 8, 14), 8m, isManual: false);
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Active);

        using var _ = new AssertionScope();
        result.PriceOnlyReturn.Should().NotBeNull();
        result.TotalReturn.Should().NotBeNull();
        result.TotalReturn.Should().BeGreaterThan(result.PriceOnlyReturn!.Value, "the total return series also carries the dividend the price-only series does not");
    }

    [Fact]
    public void GetValuation_UsesTheAsOfDateThePriceWasRecordedOn_NotAnExactDateMatchRequirement()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.SetPrice(new DateOnly(2026, 8, 10), 8m, isManual: false); // four days before FakeTimeProvider's "today"
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Active);

        result.MarketValue.Should().Be(80m);
        result.PriceAsOfDate.Should().Be(new DateOnly(2026, 8, 10));
    }

    [Fact]
    public void GetValuation_Historic_MarketValueIsZeroNotNull()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Historic);

        result.MarketValue.Should().Be(0m);
    }

    [Fact]
    public void GetValuation_Historic_NeverReportsUnrealisedGain()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Historic);

        result.UnrealisedGain.Should().BeNull();
    }

    [Fact]
    public void GetValuation_Historic_IgnoresAnyRecordedPrice()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.SetPrice(new DateOnly(2026, 8, 14), 8m, isManual: false);
        var service = CreateService();

        var result = service.GetValuation(asset, InvestmentScope.Historic);

        result.PriceAsOfDate.Should().BeNull();
        result.MarketValue.Should().Be(0m);
    }

    [Fact]
    public void GetValuation_ValidRequest_RecordsSuccessfulSpan()
    {
        var asset = MakeAsset();
        var tracer = new RecordingTelemetryTracer();
        var service = CreateService(tracer);

        service.GetValuation(asset, InvestmentScope.Active);

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("Investment.HoldingValuationService.GetValuation");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("Investment");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("HoldingValuation");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public void GetValuation_NullAsset_RecordsFailedSpanWithoutLogging()
    {
        var tracer = new RecordingTelemetryTracer();
        var logger = new RecordingLogger<HoldingValuationService>();
        var service = new HoldingValuationService(new XirrCalculationService(), tracer, logger, new FakeTimeProvider(Today));

        Action act = () => service.GetValuation(null!, InvestmentScope.Active);
        act.Should().Throw<ArgumentNullException>();

        var span = tracer.Spans.Should().ContainSingle().Which;
        using (new AssertionScope())
        {
            span.RecordedException.Should().BeOfType<ArgumentNullException>();
            logger.Entries.Should().NotContain(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Error);
        }
    }
}
