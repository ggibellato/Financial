using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class PortfolioAssetSummaryServiceTests
{
    private readonly StubActivePortfolioAssetSummaryService _activeService = new();
    private readonly StubHistoricPortfolioAssetSummaryService _historicService = new();

    [Fact]
    public void Constructor_WithNullActiveService_Throws()
    {
        Action act = () => new PortfolioAssetSummaryService(null!, _historicService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activePortfolioAssetSummaryService");
    }

    [Fact]
    public void Constructor_WithNullHistoricService_Throws()
    {
        Action act = () => new PortfolioAssetSummaryService(_activeService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("historicPortfolioAssetSummaryService");
    }

    [Fact]
    public void GetPortfolioAssetsSummary_DefaultScope_DelegatesToActiveService()
    {
        CreateService().GetPortfolioAssetsSummary("XPI", "Default");

        _activeService.LastRequest.Should().Be(("XPI", "Default"));
        _historicService.LastRequest.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ScopeActive_DelegatesToActiveService()
    {
        CreateService().GetPortfolioAssetsSummary("XPI", "Default", InvestmentScope.Active);

        _activeService.LastRequest.Should().Be(("XPI", "Default"));
        _historicService.LastRequest.Should().BeNull();
    }

    [Fact]
    public void GetPortfolioAssetsSummary_ScopeHistoric_DelegatesToHistoricService()
    {
        CreateService().GetPortfolioAssetsSummary("XPI", "Uncategorized", InvestmentScope.Historic);

        _historicService.LastRequest.Should().Be(("XPI", "Uncategorized"));
        _activeService.LastRequest.Should().BeNull();
    }

    private PortfolioAssetSummaryService CreateService() => new(_activeService, _historicService);

    private sealed class StubActivePortfolioAssetSummaryService : IActivePortfolioAssetSummaryService
    {
        public (string BrokerName, string PortfolioName)? LastRequest { get; private set; }

        public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
        {
            LastRequest = (brokerName, portfolioName);
            return [];
        }
    }

    private sealed class StubHistoricPortfolioAssetSummaryService : IHistoricPortfolioAssetSummaryService
    {
        public (string BrokerName, string PortfolioName)? LastRequest { get; private set; }

        public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
        {
            LastRequest = (brokerName, portfolioName);
            return [];
        }
    }
}
