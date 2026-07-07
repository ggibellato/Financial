using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IBrokerBreakdownQueryService
{
    IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName);
}
