using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class TransactionReplayOrder
{
    public static IEnumerable<Transaction> Sort(IEnumerable<Transaction> transactions) =>
        transactions.OrderBy(t => t.Date).ThenBy(IsDecrease);

    public static bool IsInOrder(Transaction earlier, Transaction incoming)
    {
        var byDate = earlier.Date.CompareTo(incoming.Date);
        if (byDate != 0)
        {
            return byDate < 0;
        }

        return IsDecrease(earlier).CompareTo(IsDecrease(incoming)) <= 0;
    }

    private static bool IsDecrease(Transaction transaction) =>
        TransactionTypeEffects.For(transaction.Type).Quantity == QuantityEffect.Decrease;
}
