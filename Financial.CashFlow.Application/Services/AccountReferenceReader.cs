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
        foreach (var expense in repository.GetExpenses())
        {
            if (expense.PaymentSourceAccount is not null)
            {
                yield return expense.PaymentSourceAccount.Id;
            }
            if (expense.CreditCard is not null)
            {
                yield return expense.CreditCard.Id;
            }
        }
        foreach (var statement in repository.GetCardStatements())
        {
            yield return statement.CreditCard.Id;
        }
        foreach (var investmentAccount in repository.GetInvestmentAccounts())
        {
            if (investmentAccount.CreditCard is not null)
            {
                yield return investmentAccount.CreditCard.Id;
            }
            if (investmentAccount.PotsAccount is not null)
            {
                yield return investmentAccount.PotsAccount.Id;
            }
        }
        foreach (var transfer in repository.GetTransfers())
        {
            yield return transfer.SourceAccount.Id;
            yield return transfer.DestinationAccount.Id;
        }
        foreach (var potsOwner in repository.GetAccounts().Where(account => account.Pots is { Count: > 0 }))
        {
            yield return potsOwner.Id;
        }
    }
}
