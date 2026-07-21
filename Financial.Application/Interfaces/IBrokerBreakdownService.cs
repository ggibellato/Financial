using Financial.Application.DTOs;
using Financial.Application.Enums;

namespace Financial.Application.Interfaces;

public interface IBrokerBreakdownService
{
    IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active);
}
