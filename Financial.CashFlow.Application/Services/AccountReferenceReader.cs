using Financial.CashFlow.Application.Interfaces;

namespace Financial.CashFlow.Application.Services;

internal static class AccountReferenceReader
{
    internal static HashSet<Guid> ReferencedAccountIds(ICashFlowRepository repository)
    {
        var ids = new HashSet<Guid>();
        foreach (var adjustment in repository.GetBalanceAdjustments())
        {
            ids.Add(adjustment.Account.Id);
        }
        foreach (var income in repository.GetIncomes().Where(income => income.Account is not null))
        {
            ids.Add(income.Account!.Id);
        }
        foreach (var expense in repository.GetExpenses().Where(expense => expense.PaymentSourceAccount is not null))
        {
            ids.Add(expense.PaymentSourceAccount!.Id);
        }
        foreach (var transfer in repository.GetTransfers())
        {
            ids.Add(transfer.SourceAccount.Id);
            ids.Add(transfer.DestinationAccount.Id);
        }
        return ids;
    }

    internal static bool IsReferenced(ICashFlowRepository repository, Guid accountId) =>
        ReferencedAccountIds(repository).Contains(accountId);
}
