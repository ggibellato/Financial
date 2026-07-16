using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class BrokerBreakdownService : IBrokerBreakdownService
{
    private readonly IActiveBrokerBreakdownService _activeBrokerBreakdownService;
    private readonly IHistoricBrokerBreakdownService _historicBrokerBreakdownService;

    public BrokerBreakdownService(
        IActiveBrokerBreakdownService activeBrokerBreakdownService,
        IHistoricBrokerBreakdownService historicBrokerBreakdownService)
    {
        _activeBrokerBreakdownService = activeBrokerBreakdownService ?? throw new ArgumentNullException(nameof(activeBrokerBreakdownService));
        _historicBrokerBreakdownService = historicBrokerBreakdownService ?? throw new ArgumentNullException(nameof(historicBrokerBreakdownService));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active) =>
        scope == InvestmentScope.Historic
            ? _historicBrokerBreakdownService.GetBrokerBreakdown(brokerName)
            : _activeBrokerBreakdownService.GetBrokerBreakdown(brokerName);
}
