using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ITransactionQueryService
{
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName);
    IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName);
}
