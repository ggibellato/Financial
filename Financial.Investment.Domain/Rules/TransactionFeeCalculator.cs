using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class TransactionFeeCalculator
{
    /// <summary>
    /// Recovers the fee implied by a recorded total, following the transaction direction: a
    /// purchase's total paid exceeds the gross amount by the fee, while a sale's total received
    /// falls short of it by the fee.
    /// <para>
    /// Returns the raw figure, negative included. A negative result is not a fee - it means the
    /// recorded total disagrees with quantity times unit price - so a caller that can report bad
    /// source data sees the anomaly before <see cref="Transaction.CreateFromTotal"/> floors it away.
    /// </para>
    /// </summary>
    public static decimal DeriveFromTotal(
        Transaction.TransactionType type,
        decimal quantity,
        decimal unitPrice,
        decimal totalAmount)
    {
        var grossAmount = unitPrice * quantity;
        return type == Transaction.TransactionType.Buy ? totalAmount - grossAmount : grossAmount - totalAmount;
    }
}
