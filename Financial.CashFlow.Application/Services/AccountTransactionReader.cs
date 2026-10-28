using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Application.Services;

internal static class AccountTransactionReader
{
    internal static IReadOnlyList<IAccountTransaction> Read(ICashFlowRepository repository) =>
    [
        .. repository.GetIncomes(),
        .. repository.GetExpenses(),
        .. repository.GetTransfers(),
        .. repository.GetBalanceAdjustments(),
        .. repository.GetReserveMovements()
    ];
}
