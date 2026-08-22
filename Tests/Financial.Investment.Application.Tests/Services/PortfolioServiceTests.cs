using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class PortfolioServiceTests
{
    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<PortfolioService> _logger = new();

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_WhenEmpty_RemovesItAndPersistsOnce()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        await CreateService().DeleteEmptyPortfolioAsync("XPI", "Stale", InvestmentScope.Active);

        using (new AssertionScope())
        {
            _repository.Broker!.FindPortfolio("Stale").Should().BeNull();
            _repository.Broker.FindPortfolio("Default").Should().NotBeNull();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_WhenItStillHoldsAssets_IsRefusedAndNothingIsWritten()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        var act = async () => await CreateService().DeleteEmptyPortfolioAsync("XPI", "Default", InvestmentScope.Active);

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        using (new AssertionScope())
        {
            _repository.WriteCallCount.Should().Be(0, "a refused deletion must never reach the file");
            _repository.Broker!.FindPortfolio("Default").Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_WhenTheBrokerIsUnknown_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        var act = async () => await CreateService().DeleteEmptyPortfolioAsync("Nope", "Stale", InvestmentScope.Active);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_WhenThePortfolioIsUnknown_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        var act = async () => await CreateService().DeleteEmptyPortfolioAsync("XPI", "Nope", InvestmentScope.Active);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", "Stale")]
    [InlineData("   ", "Stale")]
    [InlineData("XPI", "")]
    [InlineData("XPI", "   ")]
    public async Task DeleteEmptyPortfolioAsync_WithABlankName_ThrowsArgumentException(string broker, string portfolio)
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        var act = async () => await CreateService().DeleteEmptyPortfolioAsync(broker, portfolio, InvestmentScope.Active);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_UsesTheScopeItWasGiven()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        await CreateService().DeleteEmptyPortfolioAsync("XPI", "Stale", InvestmentScope.Historic);

        _repository.LastGetBrokerListScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public async Task DeleteEmptyPortfolioAsync_WhenRefused_MarksTheSpanFailedWithoutLoggingTheName()
    {
        _repository.Broker = CreateBrokerWithEmptyPortfolio();

        var act = async () => await CreateService().DeleteEmptyPortfolioAsync("XPI", "Default", InvestmentScope.Active);
        await act.Should().ThrowAsync<InvestmentRuleViolationException>();

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name.EndsWith("DeleteEmptyPortfolio")).Which;
        using (new AssertionScope())
        {
            span.RecordedException.Should().BeOfType<InvestmentRuleViolationException>();
            _logger.Entries.Should().NotContain(entry => entry.Message.Contains("Default"));
        }
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        var act = () => new PortfolioService(null!, _tracer, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        var act = () => new PortfolioService(_repository, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var act = () => new PortfolioService(_repository, _tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    private PortfolioService CreateService() => new(_repository, _tracer, _logger);

    /// <summary>"Default" holds an asset; "Stale" is the emptied one a move would have left behind.</summary>
    private static Broker CreateBrokerWithEmptyPortfolio()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(Asset.Create("AAAA", "ISIN123", "NYSE", "AAA"));
        broker.AddPortfolio("Stale");
        return broker;
    }
}
