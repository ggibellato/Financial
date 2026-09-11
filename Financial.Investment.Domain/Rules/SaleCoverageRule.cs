using System.Collections.Generic;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record SaleCoverageViolation(Transaction OffendingSale, decimal QuantityHeld, decimal Shortfall);

public static class SaleCoverageRule
{
    public static SaleCoverageViolation? FindFirstUncoveredSale(IEnumerable<Transaction> transactions)
    {
        var quantity = 0m;

        foreach (var transaction in TransactionReplayOrder.Sort(transactions))
        {
            if (transaction.Type == Transaction.TransactionType.Buy)
            {
                quantity += transaction.Quantity;
                continue;
            }

            if (transaction.Quantity > quantity)
            {
                return new SaleCoverageViolation(transaction, quantity, transaction.Quantity - quantity);
            }

            quantity -= transaction.Quantity;
        }

        return null;
    }
}
