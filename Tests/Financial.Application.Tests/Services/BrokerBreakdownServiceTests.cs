using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class BrokerBreakdownServiceTests
{
    private readonly StubActiveBrokerBreakdownService _activeService = new();
    private readonly StubHistoricBrokerBreakdownService _historicService = new();

    [Fact]
    public void Constructor_WithNullActiveService_Throws()
    {
        Action act = () => new BrokerBreakdownService(null!, _historicService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activeBrokerBreakdownService");
    }

    [Fact]
    public void Constructor_WithNullHistoricService_Throws()
    {
        Action act = () => new BrokerBreakdownService(_activeService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("historicBrokerBreakdownService");
    }

    [Fact]
    public void GetBrokerBreakdown_DefaultScope_DelegatesToActiveService()
    {
        CreateService().GetBrokerBreakdown("XPI");

        _activeService.LastRequestedBrokerName.Should().Be("XPI");
        _historicService.LastRequestedBrokerName.Should().BeNull();
    }

    [Fact]
    public void GetBrokerBreakdown_ScopeActive_DelegatesToActiveService()
    {
        CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Active);

        _activeService.LastRequestedBrokerName.Should().Be("XPI");
        _historicService.LastRequestedBrokerName.Should().BeNull();
    }

    [Fact]
    public void GetBrokerBreakdown_ScopeHistoric_DelegatesToHistoricService()
    {
        CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Historic);

        _historicService.LastRequestedBrokerName.Should().Be("XPI");
        _activeService.LastRequestedBrokerName.Should().BeNull();
    }

    [Fact]
    public void GetBrokerBreakdown_ScopeHistoric_ReturnsHistoricServiceResult()
    {
        var expected = new List<PortfolioBreakdownItemDTO> { new() { PortfolioName = "Uncategorized" } };
        _historicService.Result = expected;

        var result = CreateService().GetBrokerBreakdown("XPI", InvestmentScope.Historic);

        result.Should().BeSameAs(expected);
    }

    private BrokerBreakdownService CreateService() => new(_activeService, _historicService);

    private sealed class StubActiveBrokerBreakdownService : IActiveBrokerBreakdownService
    {
        public string? LastRequestedBrokerName { get; private set; }
        public IReadOnlyList<PortfolioBreakdownItemDTO> Result { get; set; } = [];

        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
        {
            LastRequestedBrokerName = brokerName;
            return Result;
        }
    }

    private sealed class StubHistoricBrokerBreakdownService : IHistoricBrokerBreakdownService
    {
        public string? LastRequestedBrokerName { get; private set; }
        public IReadOnlyList<PortfolioBreakdownItemDTO> Result { get; set; } = [];

        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
        {
            LastRequestedBrokerName = brokerName;
            return Result;
        }
    }
}
