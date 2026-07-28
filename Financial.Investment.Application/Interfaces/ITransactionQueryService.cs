using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface ITransactionQueryService
{
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active);
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
