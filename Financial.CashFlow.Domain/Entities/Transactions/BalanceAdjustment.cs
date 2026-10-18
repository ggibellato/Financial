using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class BalanceAdjustment : IAccountTransaction
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public Account Account { get; private set; } = null!;
    public decimal TargetBalance { get; private set; }
    public decimal Delta { get; private set; }
    public string? Note { get; private set; }

    private BalanceAdjustment() { }

    public static BalanceAdjustment Create(
        DateOnly date,
        Account account,
        decimal targetBalance,
        decimal delta,
        string? note)
    {
        ArgumentNullException.ThrowIfNull(account);
        Validate(targetBalance);

        if (!account.Has(Capability.CanReconcile))
        {
            throw new ArgumentException($"Account '{account.Name}' cannot be reconciled.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Account = account,
            TargetBalance = targetBalance,
            Delta = delta,
            Note = note
        };
    }

    public void UpdateDetails(DateOnly date, decimal targetBalance, decimal delta, string? note)
    {
        Validate(targetBalance);

        Date = date;
        TargetBalance = targetBalance;
        Delta = delta;
        Note = note;
    }

    public decimal ContributionTo(Account account) => Account.Id == account.Id ? Delta : 0m;

    private static void Validate(decimal targetBalance)
    {
        if (targetBalance < 0)
        {
            throw new ArgumentException("Balance cannot be negative.");
        }
    }
}
