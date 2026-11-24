using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.ValueObjects;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// Lookup tables built fresh on every <see cref="CashFlowDataConverter"/> read, resolving a
/// reference property's Id against the seeded collection deserialized earlier in the same call.
/// </summary>
public sealed class ReferenceResolutionContext
{
    public Dictionary<Guid, Account> Accounts { get; } = new();
    public Dictionary<Guid, IncomeSource> IncomeSources { get; } = new();
    public Dictionary<Guid, InvestmentAccount> InvestmentAccounts { get; } = new();
    public Dictionary<Guid, Pot> Pots { get; } = new();
    public Dictionary<Guid, Category> Categories { get; } = new();
    public Dictionary<Guid, Income> Incomes { get; } = new();
    public List<(Account Account, MoneyDestination? RoundUp, MoneyDestination? Cashback)> PendingDestinations { get; } = [];

    public void ApplyPendingDestinations()
    {
        foreach (var (account, roundUp, cashback) in PendingDestinations)
        {
            if (roundUp is not null)
            {
                account.SetRoundUpDestination(roundUp, Target(roundUp));
            }

            if (cashback is not null)
            {
                account.SetCashbackDestination(cashback, Target(cashback));
            }
        }

        PendingDestinations.Clear();
    }

    private Account? Target(MoneyDestination destination) =>
        destination.AccountId is { } id ? Accounts.GetValueOrDefault(id) : null;
}
