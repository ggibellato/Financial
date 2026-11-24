using System.Text.Json;
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
                Apply(account, "round-up", () => account.SetRoundUpDestination(roundUp, Target(roundUp)));
            }

            if (cashback is not null)
            {
                Apply(account, "cashback", () => account.SetCashbackDestination(cashback, Target(cashback)));
            }
        }

        PendingDestinations.Clear();
    }

    private static void Apply(Account account, string money, Action setDestination)
    {
        try
        {
            setDestination();
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Account {account.Id} has a {money} destination that can't receive it.", ex);
        }
    }

    private Account? Target(MoneyDestination destination) =>
        destination.AccountId is { } id ? Accounts.GetValueOrDefault(id) : null;
}
