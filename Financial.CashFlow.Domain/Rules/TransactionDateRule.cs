using System;

namespace Financial.CashFlow.Domain.Rules;

public static class TransactionDateRule
{
    private const string FutureDateMessage = "Date must not be in the future.";

    public static bool IsInFuture(DateOnly date, DateOnly today) => date > today;

    public static void EnsureNotInFuture(DateOnly date, DateOnly today)
    {
        if (IsInFuture(date, today))
        {
            throw new ArgumentException(FutureDateMessage);
        }
    }
}
