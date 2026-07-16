using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IHistoricBrokerBreakdownService
{
    IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName);
}
