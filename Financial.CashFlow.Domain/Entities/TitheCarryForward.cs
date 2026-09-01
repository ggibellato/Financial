using System;

namespace Financial.CashFlow.Domain.Entities;

/// <summary>
/// A single month's carry-forward decision: the amount available to bring in from the previous
/// month's unpaid Tithe Balance, snapshotted once and never recomputed, plus whether it currently
/// counts toward this month's Tithe Balance.
/// </summary>
public class TitheCarryForward
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal Amount { get; private set; }
    public bool Included { get; private set; }

    private TitheCarryForward() { }

    public static TitheCarryForward Create(int year, int month, decimal amount)
    {
        Validate(month, amount);

        return new()
        {
            Year = year,
            Month = month,
            Amount = amount,
            Included = true
        };
    }

    public void SetIncluded(bool included) => Included = included;

    private static void Validate(int month, decimal amount)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month must be between 1 and 12.");
        if (amount <= 0)
            throw new ArgumentException("Carry-forward amount must be positive.");
    }
}
