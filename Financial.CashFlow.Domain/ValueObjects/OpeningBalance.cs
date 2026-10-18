using System;

namespace Financial.CashFlow.Domain.ValueObjects;

public sealed record OpeningBalance
{
    public decimal Amount { get; }
    public DateOnly StartDate { get; }

    private OpeningBalance(decimal amount, DateOnly startDate)
    {
        Amount = amount;
        StartDate = startDate;
    }

    public static OpeningBalance Create(decimal amount, DateOnly startDate)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Opening balance cannot be negative.");
        }

        return new(amount, startDate);
    }
}
