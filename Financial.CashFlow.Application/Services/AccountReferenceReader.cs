using Financial.CashFlow.Application.Interfaces;

namespace Financial.CashFlow.Application.Services;

internal static class AccountReferenceReader
{
    internal static HashSet<Guid> ReferencedAccountIds(ICashFlowRepository repository) => [.. References(repository)];

    internal static bool IsReferenced(ICashFlowRepository repository, Guid accountId) =>
        References(repository).Contains(accountId);

    private static IEnumerable<Guid> References(ICashFlowRepository repository)
    {
        foreach (var adjustment in repository.GetBalanceAdjustments())
        {
            yield return adjustment.Account.Id;
        }
        foreach (var income in repository.GetIncomes().Where(income => income.Account is not null))
        {
            yield return income.Account!.Id;
        }
        foreach (var expense in repository.GetExpenses().Where(expense => expense.PaymentSourceAccount is not null))
        {
            yield return expense.PaymentSourceAccount!.Id;
        }
        foreach (var transfer in repository.GetTransfers())
        {
            yield return transfer.SourceAccount.Id;
            yield return transfer.DestinationAccount.Id;
        }
    }
}
