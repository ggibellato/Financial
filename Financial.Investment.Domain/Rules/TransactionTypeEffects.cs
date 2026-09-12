using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public enum QuantityEffect { Increase, Decrease, None }

public enum CashEffect { In, Out, None }

public sealed record TransactionTypeEffect(QuantityEffect Quantity, CashEffect Cash);

/// <summary>
/// Declares each <see cref="Transaction.TransactionType"/>'s quantity/cash effect as data, so a
/// future type is added by extending this switch rather than editing every consumer (validation,
/// average-price/realized-gain replay, oversell coverage, replay tie-break, invested-amount
/// bucketing, and both front ends' entry forms all key off this one table).
/// </summary>
public static class TransactionTypeEffects
{
    public static TransactionTypeEffect For(Transaction.TransactionType type) => type switch
    {
        Transaction.TransactionType.Buy => new(QuantityEffect.Increase, CashEffect.Out),
        Transaction.TransactionType.Sell => new(QuantityEffect.Decrease, CashEffect.In),
        Transaction.TransactionType.Fee => new(QuantityEffect.None, CashEffect.Out),
        Transaction.TransactionType.Redemption => new(QuantityEffect.Decrease, CashEffect.In),
        Transaction.TransactionType.TransferIn => new(QuantityEffect.Increase, CashEffect.None),
        Transaction.TransactionType.TransferOut => new(QuantityEffect.Decrease, CashEffect.None),
        Transaction.TransactionType.CapitalCall => new(QuantityEffect.None, CashEffect.Out),
        Transaction.TransactionType.ReturnOfCapital => new(QuantityEffect.None, CashEffect.In),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown transaction type.")
    };
}
