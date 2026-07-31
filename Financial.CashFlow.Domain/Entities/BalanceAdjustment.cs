using System;

namespace Financial.CashFlow.Domain.Entities;

public class BalanceAdjustment
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Bank { get; private set; } = string.Empty;
    public decimal TargetBalance { get; private set; }
    public decimal Delta { get; private set; }
    public string? Note { get; private set; }

    private BalanceAdjustment() { }

    public static BalanceAdjustment Create(
        DateOnly date,
        string bank,
        decimal targetBalance,
        decimal delta,
        string? note)
    {
        Validate(targetBalance);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Bank = bank,
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

    private static void Validate(decimal targetBalance)
    {
        if (targetBalance < 0)
        {
            throw new ArgumentException("Balance cannot be negative.");
        }
    }
}
