using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ITransactionQueryService
{
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName);
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName);
}
