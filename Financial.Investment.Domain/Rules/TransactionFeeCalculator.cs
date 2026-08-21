using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class TransactionFeeCalculator
{
    /// <summary>
    /// Recovers the fee folded into a recorded total, following the transaction direction: a
    /// purchase's total paid exceeds the gross amount by the fee, while a sale's total received
    /// falls short of it by the fee. The inverse of <see cref="Transaction.TotalPrice"/>, which
    /// computes the total from a known fee.
    /// <para>
    /// Returns the raw figure, negative included. A negative result is not a fee - it means the
    /// recorded total disagrees with quantity times unit price - so a caller that can report bad
    /// source data sees the anomaly before <see cref="Transaction"/> floors it away.
    /// </para>
    /// </summary>
    public static decimal RecoverFee(
        Transaction.TransactionType type,
        decimal quantity,
        decimal unitPrice,
        decimal totalPrice)
    {
        var grossAmount = unitPrice * quantity;
        return type == Transaction.TransactionType.Buy ? totalPrice - grossAmount : grossAmount - totalPrice;
    }
}
