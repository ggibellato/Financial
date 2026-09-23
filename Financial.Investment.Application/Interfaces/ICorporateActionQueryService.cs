using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface ICorporateActionQueryService
{
    IReadOnlyList<CorporateActionSummaryItemDTO> GetCorporateActionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
