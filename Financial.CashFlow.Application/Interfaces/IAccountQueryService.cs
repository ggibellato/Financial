using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAccountQueryService
{
    IReadOnlyList<AccountDTO> GetAccounts();
    AccountDetailDTO GetAccount(Guid id, int year, int month);
    IReadOnlyList<AccountTransactionDTO> GetTransactions(Guid id, int year, int month);
    IReadOnlyList<AccountStatementDTO> GetStatements(Guid id);
}
