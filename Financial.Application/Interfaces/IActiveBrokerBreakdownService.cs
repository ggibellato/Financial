using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IActiveBrokerBreakdownService
{
    IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName);
}
