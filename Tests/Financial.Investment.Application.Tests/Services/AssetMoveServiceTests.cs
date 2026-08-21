using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

public class AssetMoveServiceTests
{
    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<AssetMoveService> _logger = new();

    [Fact]
    public async Task MoveAssetAsync_ValidRequest_MovesTheAssetAndPersistsOnce()
    {
        _repository.Broker = CreateBrokerWithAsset();

        var result = await CreateService().MoveAssetAsync(CreateRequest());

        using (new AssertionScope())
        {
            result.Name.Should().Be("AAAA");
            _repository.Broker!.FindPortfolio("ISA")!.Assets.Should().ContainSingle();
            _repository.Broker.FindPortfolio("Default")!.Assets.Should().BeEmpty();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task MoveAssetAsync_WhenTheDestinationAlreadyHoldsThatAsset_IsRefusedAndNothingIsWritten()
    {
        var broker = CreateBrokerWithAsset();
        broker.AddPortfolio("ISA").AddAsset(Asset.Create("AAAA", "ISIN999", "LSE", "ZZZ"));
        _repository.Broker = broker;

        var act = async () => await CreateService().MoveAssetAsync(CreateRequest());

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0, "a refused move must never reach the file");
    }

    [Fact]
    public async Task MoveAssetAsync_WhenBrokerIsUnknown_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Broker = CreateBrokerWithAsset();

        var request = CreateRequest();
        request.BrokerName = "Nope";
        var act = async () => await CreateService().MoveAssetAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task MoveAssetAsync_WithAnUnrecognisedScope_ThrowsArgumentException()
    {
        _repository.Broker = CreateBrokerWithAsset();

        var request = CreateRequest();
        request.Scope = "sideways";
        var act = async () => await CreateService().MoveAssetAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task MoveAssetAsync_ReadsTheAssetBackFromItsDestination()
    {
        _repository.Broker = CreateBrokerWithAsset();

        var result = await CreateService().MoveAssetAsync(CreateRequest());

        result.PortfolioName.Should().Be("ISA");
    }

    [Fact]
    public async Task MoveAssetAsync_OnSuccess_MarksTheSpanSuccessful()
    {
        _repository.Broker = CreateBrokerWithAsset();

        await CreateService().MoveAssetAsync(CreateRequest());

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name.EndsWith("MoveAsset")).Which;
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task MoveAssetAsync_WhenRefused_MarksTheSpanFailedWithoutLoggingTheReason()
    {
        var broker = CreateBrokerWithAsset();
        broker.AddPortfolio("ISA").AddAsset(Asset.Create("AAAA", "ISIN999", "LSE", "ZZZ"));
        _repository.Broker = broker;

        var act = async () => await CreateService().MoveAssetAsync(CreateRequest());
        await act.Should().ThrowAsync<InvestmentRuleViolationException>();

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name.EndsWith("MoveAsset")).Which;
        using (new AssertionScope())
        {
            span.RecordedException.Should().BeOfType<InvestmentRuleViolationException>();

            // The boundary that finally handles the exception writes the log line. Logging the
            // reason here would double every failure, and the reason names holdings the user owns.
            _logger.Entries.Should().NotContain(entry => entry.Message.Contains("AAAA"));
            _logger.Entries.Should().NotContain(entry => entry.Message.Contains("completed"));
        }
    }

    [Fact]
    public async Task ArchiveAssetAsync_MovesAClosedAssetIntoHistoricAndPersistsOnce()
    {
        _repository.Investments = CreateInvestmentsWithClosedAsset();

        var result = await CreateService().ArchiveAssetAsync(CreateArchiveRequest());

        using (new AssertionScope())
        {
            result.Name.Should().Be("VOD");
            result.PortfolioName.Should().Be("Closed");
            _repository.Investments.FindHistoricBroker("XPI")!.FindPortfolio("Closed")!.Assets.Should().ContainSingle();
            _repository.Investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.Assets.Should().BeEmpty();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task ArchiveAssetAsync_LeavesTheAssetReachableOnlyInHistoric()
    {
        // FR-020: gone from Active entirely, not merely also present in Historic.
        _repository.Investments = CreateInvestmentsWithClosedAsset();
        var navigation = CreateNavigationService();

        await CreateService().ArchiveAssetAsync(CreateArchiveRequest());

        using (new AssertionScope())
        {
            navigation.GetAssetDetails("XPI", "Closed", "VOD", InvestmentScope.Historic).Should().NotBeNull();
            navigation.GetAssetDetails("XPI", "Default", "VOD", InvestmentScope.Active).Should().BeNull();
        }
    }

    [Fact]
    public async Task ArchiveAssetAsync_WhenTheAssetStillHoldsAPosition_IsRefusedAndNothingIsWritten()
    {
        var investments = CreateInvestmentsWithClosedAsset();
        investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.FindAsset("VOD")!
            .AddTransaction(Transaction.Create(new DateTime(2024, 4, 1), Transaction.TransactionType.Buy, 3m, 5m, 0m));
        _repository.Investments = investments;

        var act = async () => await CreateService().ArchiveAssetAsync(CreateArchiveRequest());

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0, "a refused archive must never reach the file");
    }

    [Fact]
    public async Task ArchiveAssetAsync_WhenTheBrokerIsUnknown_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Investments = CreateInvestmentsWithClosedAsset();

        var request = CreateArchiveRequest();
        request.BrokerName = "Nope";
        var act = async () => await CreateService().ArchiveAssetAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ArchiveAssetAsync_WithABlankDestination_ThrowsArgumentExceptionAndWritesNothing()
    {
        _repository.Investments = CreateInvestmentsWithClosedAsset();

        var request = CreateArchiveRequest();
        request.DestinationPortfolioName = "   ";
        var act = async () => await CreateService().ArchiveAssetAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ArchiveAssetAsync_WhenRefused_MarksTheSpanFailedWithoutLoggingTheHolding()
    {
        var investments = CreateInvestmentsWithClosedAsset();
        investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.FindAsset("VOD")!
            .AddTransaction(Transaction.Create(new DateTime(2024, 4, 1), Transaction.TransactionType.Buy, 3m, 5m, 0m));
        _repository.Investments = investments;

        var act = async () => await CreateService().ArchiveAssetAsync(CreateArchiveRequest());
        await act.Should().ThrowAsync<InvestmentRuleViolationException>();

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name.EndsWith("ArchiveAsset")).Which;
        using (new AssertionScope())
        {
            span.RecordedException.Should().BeOfType<InvestmentRuleViolationException>();
            _logger.Entries.Should().NotContain(entry => entry.Message.Contains("VOD"));
        }
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        var act = () => new AssetMoveService(null!, CreateNavigationService(), _tracer, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_Throws()
    {
        var act = () => new AssetMoveService(_repository, null!, _tracer, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        var act = () => new AssetMoveService(_repository, CreateNavigationService(), null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var act = () => new AssetMoveService(_repository, CreateNavigationService(), _tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    private AssetMoveService CreateService() =>
        new(_repository, CreateNavigationService(), _tracer, _logger);

    private NavigationService CreateNavigationService() =>
        new(_repository, _tracer, NullLogger<NavigationService>.Instance);

    private static ArchiveAssetRequestDTO CreateArchiveRequest() => new()
    {
        BrokerName = "XPI",
        SourcePortfolioName = "Default",
        AssetName = "VOD",
        DestinationPortfolioName = "Closed"
    };

    /// <summary>Active XPI holding "VOD", bought then fully sold, with a Historic counterpart present.</summary>
    private static Investments CreateInvestmentsWithClosedAsset()
    {
        var investments = Investments.Create();

        var active = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("VOD", "ISIN123", "LSE", "VOD");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 10m, 7m, 0m));
        active.AddPortfolio("Default").AddAsset(asset);

        investments.AddActiveBroker(active);
        investments.AddHistoricBroker(Broker.Create("XPI", "BRL"));
        return investments;
    }

    private static MoveAssetRequestDTO CreateRequest() => new()
    {
        BrokerName = "XPI",
        Scope = "active",
        SourcePortfolioName = "Default",
        AssetName = "AAAA",
        DestinationPortfolioName = "ISA"
    };

    private static Broker CreateBrokerWithAsset()
    {
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("AAAA", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        return broker;
    }
}
