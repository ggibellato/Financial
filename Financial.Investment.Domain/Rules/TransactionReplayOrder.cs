using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class TransactionReplayOrder
{
    public static IEnumerable<Transaction> Sort(IEnumerable<Transaction> transactions) =>
        transactions.OrderBy(t => t.Date).ThenBy(t => t.Type == Transaction.TransactionType.Sell);

    public static bool IsInOrder(Transaction earlier, Transaction incoming)
    {
        var byDate = earlier.Date.CompareTo(incoming.Date);
        if (byDate != 0)
        {
            return byDate < 0;
        }

        var earlierIsSell = earlier.Type == Transaction.TransactionType.Sell;
        var incomingIsSell = incoming.Type == Transaction.TransactionType.Sell;
        return earlierIsSell.CompareTo(incomingIsSell) <= 0;
    }
}
