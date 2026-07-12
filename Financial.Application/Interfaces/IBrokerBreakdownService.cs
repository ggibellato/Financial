using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IBrokerBreakdownService
{
    IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName);
}
