using System;

namespace Financial.CashFlow.Domain.Rules;

public static class TransactionDateRule
{
    private const string FutureDateMessage = "Date must not be in the future.";

    public static void EnsureNotInFuture(DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            throw new ArgumentException(FutureDateMessage);
        }
    }
}
