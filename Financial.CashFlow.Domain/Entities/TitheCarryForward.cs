using System;

namespace Financial.CashFlow.Domain.Entities;

/// <summary>
/// A month's decision on whether the previous month's unpaid Tithe Balance counts toward its own.
/// The carried amount is always derived from the previous month's current balance, never stored.
/// </summary>
public class TitheCarryForward
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool Included { get; private set; }

    private TitheCarryForward() { }

    public static TitheCarryForward Create(int year, int month, bool included)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month must be between 1 and 12.");

        return new()
        {
            Year = year,
            Month = month,
            Included = included
        };
    }

    public void SetIncluded(bool included) => Included = included;
}
